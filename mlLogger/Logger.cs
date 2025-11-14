using System;
using System.Text;

public enum LogLevel : int
{
    Unknown = 0, Notify = 1, Warning = 2, Important = 3, Critical = 5, Fatal = 6,
    Verbose = 1
}

namespace mlLogger
{
    public class Logger
    {
        public readonly LogLevel Level;

        public Logger(LogLevel level = LogLevel.Warning)
        {
            Level = level;
        }

        public void LogException(Exception e, LogLevel level)
        {
            if (level < Level) { return; }

            Console.WriteLine($"EXCEPTION[{level}]:".ToUpper());
            Console.WriteLine(e.FormatException());

            if (e.InnerException is Exception inner)
            {
                Console.WriteLine();
                Console.WriteLine($"INNER EXCEPTION:".ToUpper());
                Console.WriteLine(inner.FormatException());
            }

            AfterLogException(e, level);
        }

        public void LogMessage(string message, LogLevel level)
        {
            if (level < Level) { return; }

            Console.WriteLine($"MESSAGE[{level}]: ".ToUpper() + message);

            AfterLogMessage(message, level);
        }

        protected virtual void AfterLogException(Exception e, LogLevel level) { }
        protected virtual void AfterLogMessage(string message, LogLevel level) { }
    }
}
