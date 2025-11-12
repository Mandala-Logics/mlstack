using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DDEncoder;
using mlAutoCollection;
using mlStringValidation.Path;

namespace mlStackLib
{
    public readonly struct FileSearchResult
    {
        public string OriginalPath { get; }
        public string FileName { get; }
        public string LevelID { get; }
        public string BulkID { get; }
        public DateTime TimeAddedtoLevel { get; }

        internal FileSearchResult(StackedFileInfo sfi, LevelInfo li)
        {
            OriginalPath = sfi.OriginalPath;
            FileName = sfi.FileName;
            LevelID = li.ID;
            BulkID = sfi.BulkID;
            TimeAddedtoLevel = li.TimeSaved;
        }

        public PathBase GetPath(PathStructure ps)
        {
            return (PathBase)ps.DefaultConstructor.Invoke(new object[] { OriginalPath, DestType.File });
        }

        public PathBase GetPath(PathBase examplePath)
        {
            return (PathBase)examplePath.Structure.DefaultConstructor.Invoke(new object[] { OriginalPath, DestType.File });
        }

        public override string ToString()
        {
            return OriginalPath;
        }
    }

    public readonly struct BulkDataInfo : IEncodable
    {
        public string BulkID { get; }
        public int[] Hash { get; }
        public long Length { get; }

        internal BulkDataInfo(string bulkID, int[] hash, long length)
        {
            BulkID = bulkID;
            Hash = hash;
            Length = length;
        }

        public BulkDataInfo(EncodedObject eo)
        {
            BulkID = eo.Next<string>();
            Hash = eo.Next<int[]>();
            Length = eo.Next<long>();
        }

        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(BulkID);
            encodedObj.Append(Hash);
            encodedObj.Append(Length);
        }
    }

    public readonly struct StackedFileInfo : IEncodable
    {
        public string BulkID { get; }
        public string OriginalPath { get; }
        public string FileName { get; }
        public string LevelID { get; }

        public StackedFileInfo(string bulkID, PathBase path, string levelID)
        {
            BulkID = bulkID;
            OriginalPath = path.Path;
            FileName = path.EndPointName;
            LevelID = levelID;
        }

        public StackedFileInfo(EncodedObject eo)
        {
            BulkID = eo.Next<string>();
            OriginalPath = eo.Next<string>();
            FileName = eo.Next<string>();
            LevelID = eo.Next<string>();
        }

        public void Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(BulkID);
            encodedObj.Append(OriginalPath);
            encodedObj.Append(FileName);
            encodedObj.Append(LevelID);
        }
    }

    public sealed class LevelInfo : IReadOnlyList<StackedFileInfo>, IEncodable
    {
        public StackedFileInfo this[int index] => ((IReadOnlyList<StackedFileInfo>)files)[index];
        public DateTime TimeSaved { get; }
        public int Count => ((IReadOnlyCollection<StackedFileInfo>)files).Count;
        public string ID { get; }
        public IEncodable? Metadata { get; }
        public bool HasMetadata => Metadata is IEncodable;

        private readonly List<StackedFileInfo> files;

        internal LevelInfo(string id, int count, IEncodable? metadata)
        {
            TimeSaved = DateTime.Now;
            files = new List<StackedFileInfo>(count);
            ID = id;
            Metadata = metadata;
        }

        public LevelInfo(EncodedObject eo)
        {
            TimeSaved = eo.Next<DateTime>();

            var f = eo.Next<StackedFileInfo[]>();

            files = f.ToList();

            ID = eo.Next<string>();

            if (eo.Next<bool>())
            {
                Metadata = eo.Next<IEncodable>();
            }
        }

        internal void AppendFile(string bulkID, PathBase path)
        {
            files.Add(new StackedFileInfo(bulkID, path, ID));
        }

        public IEnumerator<StackedFileInfo> GetEnumerator()
        {
            return ((IEnumerable<StackedFileInfo>)files).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)files).GetEnumerator();
        }

        void IEncodable.Encode(ref EncodedObject encodedObj)
        {
            encodedObj.Append(TimeSaved);
            encodedObj.Append(files);
            encodedObj.Append(ID);

            encodedObj.Append(HasMetadata);

            if (HasMetadata)
            {
                encodedObj.Append(Metadata);
            }
        }

        public override string ToString()
        {
            return $"[{ID}] - {TimeSaved}";
        }
    }
}