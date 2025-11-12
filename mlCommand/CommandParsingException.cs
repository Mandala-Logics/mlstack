using System;
using System.Linq.Expressions;

namespace mlCommand
{
    public sealed class CommandParsingException : Exception
    {
        public ShellOutput ShellOutput { get; }
        public int Line { get; } = -1;

        public CommandParsingException(string message) : base(message)
        {
        }

        public CommandParsingException(int line, ShellOutput output) : base(output.StandardOutput)
        {
            ShellOutput = output;
            Line = line;
        }

        public CommandParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public sealed class WrongImplimentationException : Exception
    {
        public WrongImplimentationException(string message) : base(message) {}
    }
}