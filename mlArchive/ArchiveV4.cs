//#define debug

using mlAutoCollection;
using mlAutoCollection.Sync;
using mlFileInterface;
using mlStringValidation;
using mlStringValidation.Path;
using mlThreadMGMT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ArcV4
{
    public partial class ArchiveV4 : ICollectable, IEquatable<ArchiveV4>, IThreadManager
    {
        //STATIC PROPERTIES
        public static StringTemplate ArchiveNameTemplate => ArchiveFileHeader.ArchiveNameTemplate;

        //PUBLIC PROPERTIES
        public string ArchiveName => FileHeader.ArchiveName;
        public ArchiveRoot Root { get; }
        public ArcPath RootPath { get; private set; }
        public long FileLength => blocks[FileHeader.BlockCount - 1].BlockRange.EndPosition;
        public PathBase ArchivePath { get; }
        public bool Disposed => Interface?.Disposed ?? true;
        public bool Packaged => FileHeader.Packaged;
        public float Progress => Interface.Progress;

        //INTERNAL PROPERTIES
        internal BlockTableEntry this[int index] => blocks[index]; 
        internal ArchiveFileHeader FileHeader { get; }
        internal FileInterface Interface { get; set; }
        internal ThreadManager Threads { get; } = new ThreadManager() { RemoveOnCompleted = true };

        //INTERFACE PROPERTIES
        object ICollectable.Key => FileHeader.ArchiveName;
        CollectionHandle ICollectable.Handle => collectionHandle;
        int IThreadManager.TotalThreads => Threads.Count;

        //PRIVATE PROPERTIES
        private readonly CollectionHandle collectionHandle = new CollectionHandle();
        private readonly AddOnlySyncedList<BlockTableEntry> blockTableRecord = new AddOnlySyncedList<BlockTableEntry>();
        private readonly AddOnlySyncedList<BlockTableEntry> blocks;
        private IOTask fileHeaderWriteTask;

        //STATIC CONTRCUTOR
        static ArchiveV4()
        {
            DDEncoder.DDEncoder.RegisterTypes(typeof(ArchiveFile), 
                typeof(ArchiveDir), 
                typeof(BlockTableEntry), 
                typeof(ArchiveFileHeader),
                typeof(FileSystemID),
                typeof(IORange));
        }

        //CONSTRCUTORS
#if debug
        public ArchiveV4() { }
#endif
        private ArchiveV4(string name, PathBase path, FileMode fileMode) //for creating archives
        {
            ArchivePath = path;
            FileHeader = new ArchiveFileHeader(collectionHandle, name);

            Root = new ArchiveRoot(this);
            RootPath = Root.Path;

            Interface = new FileInterface(path, fileMode);

            try { ArchiveManager.Arcs.Add(this); }
            catch (KeyAlreadyExistsException) 
            {
                Dispose();
                throw new NameNotAllowedException($"An archive with this name ({FileHeader.ArchiveName}) is already open."); 
            }            

            using (IOHandle handle = Interface.GetHandle())
            {
                FileHeader.BlockCount = 2;
                blocks = new AddOnlySyncedList<BlockTableEntry>(2);

                handle.Encode(FileHeader);

                BlockTableEntry bte = new BlockTableEntry(0, BlockType.BlockTable, ArchiveConstants.FileHeaderLength, ArchiveConstants.BlockTableSize);

                blocks.Add(bte);
                blockTableRecord.Add(bte);

                handle.Encode(bte);

                bte = new BlockTableEntry(1, BlockType.FileTable, bte.BlockRange.EndPosition, ArchiveConstants.DefualtFileTableLength);

                blocks.Add(bte);

                handle.Encode(bte);
            }         
        }
        internal ArchiveV4(ArchiveFileHeader header, Stream stream, bool newArc)
        {
            FileHeader = header;

            Root = new ArchiveRoot(this);
            RootPath = Root.Path;

            Interface = new FileInterface(stream);

            if (newArc)
            {
                using (IOHandle handle = Interface.GetHandle())
                {
                    FileHeader.BlockCount = 2;
                    blocks = new AddOnlySyncedList<BlockTableEntry>(2);

                    handle.Encode(FileHeader);

                    BlockTableEntry bte = new BlockTableEntry(0, BlockType.BlockTable, ArchiveConstants.FileHeaderLength, ArchiveConstants.BlockTableSize);

                    blocks.Add(bte);
                    blockTableRecord.Add(bte);

                    handle.Encode(bte);

                    bte = new BlockTableEntry(1, BlockType.FileTable, bte.BlockRange.EndPosition, ArchiveConstants.DefualtFileTableLength);

                    blocks.Add(bte);

                    handle.Encode(bte);
                }
            }
            else
            {
                using (IOHandle handle = Interface.GetHandle())
                {
                    handle.Decode();
                    handle.Clear();

                    handle.Decode();

                    Root = new ArchiveRoot(this);
                    RootPath = Root.Path;

                    blocks = new AddOnlySyncedList<BlockTableEntry>(FileHeader.BlockCount);

                    BlockTableEntry table;
                    BlockTableEntry block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                    blockTableRecord.Add(block);
                    blocks.Add(block);
                    
                    int tableIndex = -1;
                    int total = 1;
                    int blocksRead = 1;                

                    do
                    {
                        table = blockTableRecord[++tableIndex];

                        handle.Position = table.BlockRange.StartPosition + (total == 1 ? ArchiveConstants.BlockTableEntryLength : 0);                    

                        do
                        {
                            handle.Decode();

                            blocksRead++;
                            total++;

                        } while (blocksRead < table.MaxBlocks && total < FileHeader.BlockCount);

                        while (!handle.Empty)
                        {
                            block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                            if (block.Type == BlockType.BlockTable) { blockTableRecord.Add(block); }

                            blocks.Add(block);
                        }

                        blocksRead = 0;

                    } while (table.NextBlock != -1);
                }

                if (blocks.Count != FileHeader.BlockCount) throw new BadBinaryException($"Blocks read ({blocks.Count}) and file header block count ({FileHeader.BlockCount}) do not match.");
            }
        }
        private ArchiveV4(string name, Stream stream) //for creating archives
        {
            FileHeader = new ArchiveFileHeader(collectionHandle, name);

            Root = new ArchiveRoot(this);
            RootPath = Root.Path;

            Interface = new FileInterface(stream);

            try { ArchiveManager.Arcs.Add(this); }
            catch (KeyAlreadyExistsException) 
            {
                Dispose();
                throw new NameNotAllowedException($"An archive with this name ({FileHeader.ArchiveName}) is already open."); 
            }            

            using (IOHandle handle = Interface.GetHandle())
            {
                FileHeader.BlockCount = 2;
                blocks = new AddOnlySyncedList<BlockTableEntry>(2);

                handle.Encode(FileHeader);

                BlockTableEntry bte = new BlockTableEntry(0, BlockType.BlockTable, ArchiveConstants.FileHeaderLength, ArchiveConstants.BlockTableSize);

                blocks.Add(bte);
                blockTableRecord.Add(bte);

                handle.Encode(bte);

                bte = new BlockTableEntry(1, BlockType.FileTable, bte.BlockRange.EndPosition, ArchiveConstants.DefualtFileTableLength);

                blocks.Add(bte);

                handle.Encode(bte);
            }        
        }
        private ArchiveV4(PathBase path) //for opening archives
        {
            Interface = new FileInterface(path, FileMode.Open);

            using (IOHandle handle = Interface.GetHandle())
            {
                FileHeader = (ArchiveFileHeader)handle.Decode().DecodedObject.Value;
                FileHeader.SetHandle(collectionHandle);

                handle.Decode();

                Root = new ArchiveRoot(this);
                RootPath = Root.Path;

                try { ArchiveManager.Arcs.Add(this); }
                catch (KeyAlreadyExistsException) 
                {
                    Dispose();
                    throw new NameNotAllowedException($"An archive with this name ({FileHeader.ArchiveName}) is already open.");
                }

                blocks = new AddOnlySyncedList<BlockTableEntry>(FileHeader.BlockCount);

                BlockTableEntry table;
                BlockTableEntry block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                blockTableRecord.Add(block);
                blocks.Add(block);
                
                int tableIndex = -1;
                int total = 1;
                int blocksRead = 1;                

                do
                {
                    table = blockTableRecord[++tableIndex];

                    handle.Position = table.BlockRange.StartPosition + (total == 1 ? ArchiveConstants.BlockTableEntryLength : 0);                    

                    do
                    {
                        handle.Decode();

                        blocksRead++;
                        total++;

                    } while (blocksRead < table.MaxBlocks && total < FileHeader.BlockCount);

                    while (!handle.Empty)
                    {
                        block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                        if (block.Type == BlockType.BlockTable) { blockTableRecord.Add(block); }

                        blocks.Add(block);
                    }

                    blocksRead = 0;

                } while (table.NextBlock != -1);
            }

            if (blocks.Count != FileHeader.BlockCount) throw new BadBinaryException($"Blocks read ({blocks.Count}) and file header block count ({FileHeader.BlockCount}) do not match.");
        }
        private ArchiveV4(Stream stream) //for opening archives
        {
            Interface = new FileInterface(stream);

            using (IOHandle handle = Interface.GetHandle())
            {
                FileHeader = (ArchiveFileHeader)handle.Decode().DecodedObject.Value;
                FileHeader.SetHandle(collectionHandle);

                handle.Decode();

                Root = new ArchiveRoot(this);
                RootPath = Root.Path;

                try { ArchiveManager.Arcs.Add(this); }
                catch (KeyAlreadyExistsException) 
                {
                    Dispose();
                    throw new NameNotAllowedException($"An archive with this name ({FileHeader.ArchiveName}) is already open.");
                }

                blocks = new AddOnlySyncedList<BlockTableEntry>(FileHeader.BlockCount);

                BlockTableEntry table;
                BlockTableEntry block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                blockTableRecord.Add(block);
                blocks.Add(block);
                
                int tableIndex = -1;
                int total = 1;
                int blocksRead = 1;                

                do
                {
                    table = blockTableRecord[++tableIndex];

                    handle.Position = table.BlockRange.StartPosition + (total == 1 ? ArchiveConstants.BlockTableEntryLength : 0);                    

                    do
                    {
                        handle.Decode();

                        blocksRead++;
                        total++;

                    } while (blocksRead < table.MaxBlocks && total < FileHeader.BlockCount);

                    while (!handle.Empty)
                    {
                        block = (BlockTableEntry)handle.WaitNext().WaitOnObject().Value;

                        if (block.Type == BlockType.BlockTable) { blockTableRecord.Add(block); }

                        blocks.Add(block);
                    }

                    blocksRead = 0;

                } while (table.NextBlock != -1);
            }

            if (blocks.Count != FileHeader.BlockCount) throw new BadBinaryException($"Blocks read ({blocks.Count}) and file header block count ({FileHeader.BlockCount}) do not match.");
        }
        ~ArchiveV4() => Dispose();

        //PUBLIC STATIC METHODS
        public static ArchiveV4 OpenArchive(PathBase path)
        {
            return new ArchiveV4(path);
        }
        public static ArchiveV4 OpenArchive(Stream stream)
        {
            return new ArchiveV4(stream);
        }
        public static ArchiveV4 CreateArchive(string archiveName, PathBase path, FileMode fileMode)
        {
            bool createNew;

            switch (fileMode)
            {
                case FileMode.Create:
                case FileMode.Truncate:
                case FileMode.CreateNew:
                    createNew = true;
                    break;
                case FileMode.Open:
                    createNew = false;
                    break;
                case FileMode.OpenOrCreate:
                    if (path.Exists) { return OpenArchive(path); }
                    else { createNew = true; }
                    break;
                default:
                    throw new ArgumentException($"The selected FileMode ({fileMode}) is not valid for opening archives.");
            }

            if (!createNew) throw new InvalidOperationException($"A new archive cannot created here because the filemode is set to {fileMode} and the file ({path}) {(path.Exists ? "already exists" : "does not exist")}.");

            if (ArchiveNameTemplate.CatchValidate(archiveName, out _) is StringValidationException e)
            {
                throw new ArgumentException("Archive name is not valid: " + e.Message);
            }

            return new ArchiveV4(archiveName, path, fileMode);
        }
        public static ArchiveV4 CreateArchive(string archiveName, Stream stream)
        {
            return new ArchiveV4(archiveName, stream);
        }

        //INTERNAL METHODS
        internal void WriteFileHeader()
        {
            FileHeader.RootDirs = Root.DirCount;
            FileHeader.RootFiles = Root.FileCount;

            fileHeaderWriteTask?.Cancel();

            fileHeaderWriteTask = Interface.Encode(SeekOrigin.Begin, 0L, FileHeader);
        }
        internal void DeallocateChainFrom(int startBlock)
        {
            if (startBlock == -1) { return; }

            Threads.StartTask((tc) =>
            {
                BlockTableEntry bte = blocks[startBlock];
                int next = bte.NextBlock;
                bte.DeallocateBlock();
                WriteBlock(bte);

                while (next != -1)
                {
                    tc.ThrowIfAborted();

                    bte = blocks[next];
                    next = bte.NextBlock;
                    bte.DeallocateBlock();
                    WriteBlock(bte);
                }

            }, ThreadTaskPriority.MustBeCompleted);
        }
        internal void DisposeBlock(int blockID)
        {
            var bte = blocks[blockID];

            bte.DeallocateBlock();

            WriteBlock(blockID);
        }
        internal BlockTableEntry GetEmptyBlock(BlockType blockType, long blockLength)
        {
            foreach (BlockTableEntry block in blocks)
            {
                if (block.Type == BlockType.Empty && block.BlockRange.Length > blockLength * 0.5)
                {
                    block.AllocateBlock(blockType);
                    return block;
                }
            }

            return CreateNewBlock(blockType, blockLength);
        }
        internal virtual BlockTableEntry CreateNewBlock(BlockType blockType, long blockLength)
        {
            BlockTableEntry bte;

            if (blockType != BlockType.BlockTable)
            {
                int maxBlocks = GetMaxBlocks();

                if (blocks.Count > maxBlocks) throw new Exception("Block count is greater than the max number of blocks?");
                else if (blocks.Count == maxBlocks - 1)
                {
                    CreateBlockTable();
                }                
            }

            bte = new BlockTableEntry(blocks.Count, blockType, blocks.Last().BlockRange.EndPosition, blockLength);

            blocks.Add(bte);

            FileHeader.BlockCount++;          

            FileHeader.Packaged = false;
            WriteFileHeader();

            return bte;
        }
        internal long FindBTEPosition(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("Block position less than zero.");            

            foreach (BlockTableEntry table in blockTableRecord)
            {
                if (index < table.MaxBlocks)
                {
                    return table.BlockRange.StartPosition + index * ArchiveConstants.BlockTableEntryLength;
                }
                else
                {
                    index -= table.MaxBlocks;
                }
            }

            throw new ArgumentOutOfRangeException($"The block index {index} is out of range.");
        }
        internal IOTask WriteBlock(int blockID) => WriteBlock(blocks[blockID]);
        internal IOTask WriteBlock(BlockTableEntry bte)
        {
            if (!bte.ShouldWrite || Disposed) { return IOTask.Completed; }            

            if (bte.WriteTask is IOTask task)
            {
                task.TryCancel();            
            }

            return bte.WriteTask = Interface.Encode(SeekOrigin.Begin, FindBTEPosition(bte.BlockRange.Key), bte);            
        }
        internal void CreateBlockTable()
        {
            BlockTableEntry bte;

            bte = GetEmptyBlock(BlockType.BlockTable, ArchiveConstants.BlockTableSize);

            var prev = blockTableRecord[blockTableRecord.Count - 1];

            prev.SetNextBlock(bte.BlockID);
            blockTableRecord.Add(bte);

            WriteBlock(prev);
            WriteBlock(bte);
        }
        internal int GetMaxBlocks()
        {
            int ret = 0;

            foreach (BlockTableEntry table in blockTableRecord) { ret += table.MaxBlocks; }

            return ret;
        }
        internal ArchiveStream GetStream(int startBlock) => new ArchiveStream(this, startBlock);

        //PULIC METHODS
#if debug
        public void TestWrite(Stream stream)
        {
            var bte = new BlockTableEntry(33, BlockType.BlockTable, 1024, 1024);

            using (DDEncoder enc = new DDEncoder(stream)) { enc.Write(bte); }
        }
#endif
        public void Dispose()
        {
            if (!Disposed) ArchiveManager.OnDisposed(this);

            Threads?.Dispose();

            Interface?.Dispose();   

            FileHeader.BlockCount = 0;

            try { fileHeaderWriteTask?.Wait(); }
            catch (TaskCanceledException) { }

            GC.SuppressFinalize(this);
        }
        public void RenameArchive(string newName)
        {
            FileHeader.SetName(newName);
            WriteFileHeader();
            RootPath = Root.Path;
        }

        //OBJECT OVERRIDES
        public override bool Equals(object obj) => Equals(obj as ArchiveV4);
        public bool Equals(ArchiveV4 other)
        {
            return other != null && EqualityComparer<PathBase>.Default.Equals(ArchivePath, other.ArchivePath);
        }
        public override int GetHashCode()
        {
            return 238841126 + EqualityComparer<PathBase>.Default.GetHashCode(ArchivePath);
        }

        //INTERFACE METHODS
        List<ThreadStatus> IThreadManager.GetThreadsStatus() => Threads.GetThreadsStatus();
        int IThreadManager.PriorityCount(ThreadTaskPriority priority) => ((IThreadManager)Threads).PriorityCount(priority);
    }
}
