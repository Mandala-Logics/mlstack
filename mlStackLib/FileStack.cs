using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ArcV4;
using DDEncoder;
using mlAutoCollection.Cast;
using mlEncodedDB;
using mlStringValidation.Path;

namespace mlStackLib
{
    public sealed class FileStack
    {
        private readonly PathBase bulkDir;
        private readonly BlockList bulkDB;
        private readonly BlockList levelDB;
        private ArchiveV4 arc;

        public BlockList Metadata { get; }

        static FileStack()
        {
            DDEncoder.DDEncoder.RegisterTypes(typeof(BulkDataInfo), typeof(LevelInfo), typeof(StackedFileInfo));
        }

        private FileStack(ArchiveV4 stack)
        {
            arc = stack;

            bulkDir = stack.RootPath.Append("bulk", DestType.Dir);

            bulkDB = new BlockList(stack.RootPath.Append("bulk_db", DestType.File), FileMode.OpenOrCreate);

            levelDB = new BlockList(stack.RootPath.Append("level_db", DestType.File), FileMode.OpenOrCreate);

            Metadata = new BlockList(stack.RootPath.Append("metadata", DestType.File), FileMode.OpenOrCreate);
        }

        public List<FileSearchResult> FindFile(Regex pattern, bool fileNamesOnly)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            var ret = new List<FileSearchResult>();

            foreach (var ienc in levelDB)
            {
                var li = (LevelInfo)ienc;

                foreach (var sfi in li)
                {
                    if (fileNamesOnly)
                    {
                        if (pattern.IsMatch(sfi.FileName)) { ret.Add(new FileSearchResult(sfi, li)); }
                    }
                    else
                    {
                        if (pattern.IsMatch(sfi.OriginalPath)) { ret.Add(new FileSearchResult(sfi, li)); }
                    }
                }
            }

            return ret;
        }

        public List<FileSearchResult> FindFile(Regex pattern, string levelID, bool fileNamesOnly)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            var li = GetLevel(levelID);

            var ret = new List<FileSearchResult>();

            foreach (var sfi in li)
            {
                if (fileNamesOnly)
                {
                    if (pattern.IsMatch(sfi.FileName)) { ret.Add(new FileSearchResult(sfi, li)); }
                }
                else
                {
                    if (pattern.IsMatch(sfi.OriginalPath)) { ret.Add(new FileSearchResult(sfi, li)); }
                }
            }

            return ret;
        }
        
        public void RetriveFile(string bulkID, Stream outputStream)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("Output stream does not allow writing.");
            }

            var path = GetBulkFile(bulkID);

            var stream = path.OpenStream(FileMode.Open, FileAccess.Read, FileShare.None);

            stream.CopyTo(outputStream);
        }

        public void CreateLevel(IEnumerable<PathBase> paths, IEncodable? metadata)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            int c = 0;

            foreach (var path in paths)
            {
                if (!path.IsFile) { throw new ArgumentException("Paths to use to create map cannot contain a path to a dir."); }
                c++;
            }

            if (c == 0) { throw new ArgumentException("List of paths cannot be empty."); }

            string id;

            var li = new LevelInfo(GetNewLevelID(), c, metadata);

            foreach (var path in paths)
            {
                var length = path.FileLength();
                var hash = path.Hash();

                if (FindBulkID(length, hash) is string a)
                {
                    id = a;
                }
                else
                {
                    id = GetNewBulkID();

                    try { AddFileToBulk(path, hash, length, id); }
                    catch (PathAccessException)
                    {
                        throw new PathAccessException($"Could not open source file '{path}', file may already be open.");
                    }
                }

                li.AppendFile(id, path);
            }

            levelDB.Add(li);
        }

        public List<BulkDataInfo> GetAllBulkData()
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            var ret = new List<BulkDataInfo>(bulkDB.Count);

            foreach (var ienc in bulkDB)
            {
                ret.Add((BulkDataInfo)ienc);
            }

            return ret;
        }

        public List<LevelInfo> GetAllLevels()
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            var ret = new List<LevelInfo>(levelDB.Count);

            foreach (var ienc in levelDB)
            {
                ret.Add((LevelInfo)ienc);
            }

            return ret;
        }

        public LevelInfo GetLevel(string levelID)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            foreach (var ienc in levelDB)
            {
                var li = (LevelInfo)ienc;

                if (li.ID.Equals(levelID))
                {
                    return li;
                }
            }

            throw new ArgumentException($"LevelID ({levelID}) not found in database.");
        }

        public void PruneBulk()
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            var dic = new Dictionary<string, bool>();

            foreach (var ienc in bulkDB)
            {
                var bdi = (BulkDataInfo)ienc;

                dic.Add(bdi.BulkID, false);
            }

            foreach (var ienc in levelDB)
            {
                var li = (LevelInfo)ienc;

                foreach (var sfi in li)
                {
                    dic[sfi.BulkID] = true;
                }
            }

            foreach (var kvp in dic)
            {
                if (!kvp.Value)
                {
                    DeleteBulkFile(kvp.Key);
                }
            }
        }

        public void DeleteLevel(string levelID)
        {
            if (arc.Disposed) { throw new ObjectDisposedException("filestack"); }

            int c = 0;

            foreach (var ienc in levelDB)
            {
                var li = (LevelInfo)ienc;

                if (li.ID.Equals(levelID))
                {
                    levelDB.Remove(c);
                    return;
                }

                c++;
            }

            throw new ArgumentException($"LevelID ({levelID}) not found in database.");
        }

        private void DeleteBulkFile(string bulkID)
        {
            var path = GetBulkFile(bulkID);

            path.Delete();

            int c = 0;

            foreach (var ienc in bulkDB)
            {
                var bdi = (BulkDataInfo)ienc;

                if (bdi.BulkID == bulkID)
                {
                    bulkDB.Remove(c);
                    return;
                }

                c++;
            }
        }

        private PathBase GetBulkFile(string bulkID)
        {
            PathBase path;

            try { path = bulkDir.Append(bulkID, DestType.File); }
            catch (NameNotValidException)
            {
                throw new ArgumentException($"BulkID supplied ({bulkID}) was not found in the database, ID not valid.");
            }

            if (!path.Exists)
            {
                throw new ArgumentException($"BulkID supplied ({bulkID}) was not found in the database.");
            }

            return path;
        }

        private string GetNewLevelID()
        {
            string ret;

            do
            {
                ret = DDHash.GetRandomHexString(4);

            } while (levelDB.Any((li) => { return ((LevelInfo)li).ID.Equals(ret); }));

            return ret;
        }

        private void AddFileToBulk(PathBase path, int[] hash, long length, string id)
        {
            var bdi = new BulkDataInfo(id, hash, length);

            bulkDB.Add(bdi);

            var dest = bulkDir.Append(id, DestType.File);

            path.CopyFile(dest);
        }
    
        private string GetNewBulkID()
        {
            string ret;

            do
            {
                ret = DDHash.RandomHash.ToString("x8");
            } while (bulkDB.Any((bdi) => { return ((BulkDataInfo)bdi).BulkID.Equals(ret); }));

            return ret;
        }

        private string? FindBulkID(long length, int[] hash)
        {
            foreach (var ienc in bulkDB)
            {
                var bdi = (BulkDataInfo)ienc;

                if (bdi.Length == length && CompareHashes(bdi.Hash, hash))
                {
                    return bdi.BulkID;
                }
            }

            return null;
        }

        public void Dispose()
        {
            bulkDB.Dispose();
            levelDB.Dispose();
            Metadata.Dispose();
            arc.Dispose();
        }
    
        public static bool CompareHashes(int[] hash1, int[] hash2)
        {
            if (hash1.Length != hash2.Length) { return false; }

            for (int x = 0; x < hash1.Length; x++)
            {
                if (hash1[x] != hash2[x]) { return false; }
            }

            return true;
        }

        public static FileStack CreateEmptyStack(PathBase path, FileMode mode)
        {
            var arcName = path.EndPointName;

            var arc = ArchiveV4.CreateArchive(arcName, path, mode);

            arc.RootPath.Append("bulk", DestType.Dir).CreateDirectory();

            return new FileStack(arc);
        }
        
        public static FileStack OpenStack(PathBase path)
        {
            ArchiveV4 arc;

            try { arc = ArchiveV4.OpenArchive(path); }
            catch (EncodingException)
            {
                throw new ArgumentException("The path provided does not seem to be a valid stack/archive.");
            }

            return new FileStack(arc);
        }
    }
}
