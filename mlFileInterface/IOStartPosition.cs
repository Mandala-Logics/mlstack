using System;
using System.IO;

namespace mlFileInterface
{
    internal enum StartPositionType { Position = 0, StartOfFile, EndOfFile, FollowsFrom }
    internal sealed class IOStartPosition
    {
        //PUBLIC PROPERTIES
        public StartPositionType Type { get; private set; }
        public long Offset { get; private set;  }
        public long Position { get; private set; }
        public IOTask Previous { get; private set; }

        //CONSTRUCTORS
        public IOStartPosition(StartPositionType type, long offset = 0L)
        {
            switch (type)
            {
                case StartPositionType.StartOfFile:
                    if (offset < 0) throw new ArgumentOutOfRangeException("If seeking from the begining then offset cannot be less than zero.");
                    Position = Offset = offset; 
                    break;
                case StartPositionType.EndOfFile:
                    if (offset > 0) throw new ArgumentOutOfRangeException("If seeking from the end then offset cannot be greater than zero.");
                    Offset = offset;
                    Position = -1L;
                    break;
                case StartPositionType.Position:
                    throw new ArgumentException("Please also specifiy a position index if using type = Position.");
                case StartPositionType.FollowsFrom:
                    throw new ArgumentException("Please also specifiy previous task if using type = FollowsFrom.");
                default:
                    throw new IOException("Internal error: StartPositionType not defined.");
            }
            
            Previous = null;
            Type = type;
        }
        public IOStartPosition(long position)
        {
            Type = StartPositionType.Position;

            if (position < 0L) throw new ArgumentOutOfRangeException("Position cannot be less zero.");

            Position = position;
            Offset = 0L;
            Previous = null;
        }
        public IOStartPosition(IOTask previous)
        {
            Type = StartPositionType.FollowsFrom;

            Previous = previous ?? throw new ArgumentNullException("previous");

            Offset = 0L;
            Position = -1L;
        }

        //PUBLIC FUNCTIONS
        public long GetPosition()
        {
            switch (Type)
            {
                case StartPositionType.Position:
                    return Position;
                case StartPositionType.StartOfFile:
                    return Offset;
                case StartPositionType.EndOfFile:
                    return -1L;
                case StartPositionType.FollowsFrom:

                    if (Previous.HasEndPosition) { return Previous.EndPosition; }
                    else
                    {
                        long prev = Previous.StartPosition;

                        if (Previous.stream.Length > 0 && prev > -1L)
                        {
                            return prev + Previous.stream.Length;
                        }
                        else
                        {
                            return -1L;
                        }
                    }

                default:
                    throw new IOException("Internal error: StartPositionType not defined.");
            }
        }

        //INTERNAL METHODS
        internal bool SetPosition(ref Stream stream)
        {
            switch (Type)
            {
                case StartPositionType.StartOfFile:
                    stream.Seek(Offset, SeekOrigin.Begin);
                    return true;
                case StartPositionType.EndOfFile:
                    stream.Seek(Offset, SeekOrigin.End);
                    break;
                case StartPositionType.Position:
                    stream.Position = Position;
                    return true;
                case StartPositionType.FollowsFrom:

                    if (Previous.HasEndPosition)
                    {
                        stream.Position = Previous.EndPosition;
                        return true;
                    }
                    else if (Previous.IsCancelled())
                    {
                        Position = stream.Position;
                        Type = StartPositionType.Position;
                        Offset = 0L;
                        Previous = null;

                        return false;
                    }
                    else
                    {
                        throw new IOException("Failed to get previous position from IOTask.");
                    }

                default:
                    throw new IOException("Internal error: StartPositionType not defined.");
            }

            Position = stream.Position;
            Type = StartPositionType.Position;
            Offset = 0L;
            Previous = null;

            return true;
        }
    }
}
