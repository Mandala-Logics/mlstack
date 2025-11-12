using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace mlCommand
{
    /// <summary>
    /// Represents a command-line input, parsed into a collection of command arguments.
    /// </summary>
    public readonly struct CommandLine : IReadOnlyList<CommandArgument>
    {
        // STATIC PROPERTIES
        private static readonly Regex CommandRegex = new Regex(@"((?<word>\S+)(\s*|=|,))*?(;|#|$)", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        /// <summary>
        /// Gets the username of the user who executed the command.
        /// </summary>
        public string User { get; }

        /// <summary>
        /// Gets the number of command arguments.
        /// </summary>
        public int Count => arguments.Count;

        /// <summary>
        /// Gets the command argument at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the command argument to get.</param>
        /// <returns>The command argument at the specified index.</returns>
        public CommandArgument this[int index] => arguments[index];

        // PRIVATE PROPERTIES
        private readonly List<CommandArgument> arguments;

        // CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine"/> struct from a command line string.
        /// </summary>
        /// <param name="line">The command line input to parse.</param>
        public CommandLine(string line)
        {
            var match = CommandRegex.Match(line);

            User = Environment.UserName;
            arguments = new List<CommandArgument>(match.Groups[1].Length);

            foreach (Capture arg in match.Groups[1].Captures)
            {
                if (arg.Value.StartsWith("--"))
                {
                    arguments.Add(new CommandArgument(arg.Value[2..], true));
                }
                else if (arg.Value[0] == '-')
                {
                    foreach (char c in arg.Value[1..])
                    {
                        arguments.Add(new CommandArgument(c));
                    }
                }
                else
                {
                    arguments.Add(new CommandArgument(arg.Value, false));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLine"/> struct from an enumerable collection of arguments.
        /// </summary>
        /// <param name="args">The arguments to parse.</param>
        public CommandLine(IEnumerable<string> args)
        {
            User = Environment.UserName;
            arguments = new List<CommandArgument>(args.Count());

            foreach (string arg in args)
            {
                if (arg[0] == ';' || arg[0] == '#') { break; }

                if (arg.StartsWith("--"))
                {
                    arguments.Add(new CommandArgument(arg[2..], true));
                }
                else if (arg[0] == '-')
                {
                    foreach (char c in arg[1..])
                    {
                        arguments.Add(new CommandArgument(c));
                    }
                }
                else
                {
                    arguments.Add(new CommandArgument(arg, false));
                }
            }
        }

        // PUBLIC METHODS

        /// <summary>
        /// Returns a string that represents the command line.
        /// </summary>
        /// <returns>A string that represents the command line.</returns>
        public override string ToString()
        {
            var s = new List<string>(arguments.Count);

            foreach (var arg in arguments)
            {
                s.Add(arg.ToString());
            }

            return string.Join(' ', s);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the command arguments.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the command arguments.</returns>
        public IEnumerator<CommandArgument> GetEnumerator() => arguments.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the command arguments.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the command arguments.</returns>
        IEnumerator IEnumerable.GetEnumerator() => arguments.GetEnumerator();
    }

    /// <summary>
    /// Represents an individual command argument, which can be either a switch or a plain argument.
    /// </summary>
    public readonly struct CommandArgument : IEquatable<CommandArgument>
    {
        /// <summary>
        /// Gets a value indicating whether this argument is a switch.
        /// </summary>
        public bool IsSwitch { get; }

        /// <summary>
        /// Gets the argument string.
        /// </summary>
        public string Argument { get; }

        public string Command => Argument;

        public bool IsArgument => !IsSwitch;

        /// <summary>
        /// Gets the switch string if this argument is a switch; otherwise, returns the argument string.
        /// </summary>
        public string Switch => Argument;

        /// <summary>
        /// Gets a value indicating whether this argument is a short switch (single character).
        /// </summary>
        public bool ShortSwitch => IsSwitch && Argument.Length == 1;

        // CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandArgument"/> struct.
        /// </summary>
        /// <param name="argument">The argument string.</param>
        /// <param name="isSwitch">Indicates whether the argument is a switch.</param>
        public CommandArgument(string argument, bool isSwitch)
        {
            Argument = argument;
            IsSwitch = isSwitch;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandArgument"/> struct as a short switch.
        /// </summary>
        /// <param name="argument">The character representing the switch.</param>
        public CommandArgument(char argument)
        {
            Argument = argument.ToString();
            IsSwitch = true;
        }

        /// <summary>
        /// Returns a string that represents the command argument.
        /// </summary>
        /// <returns>A string that represents the command argument.</returns>
        public override string ToString()
        {
            if (IsSwitch)
            {
                if (ShortSwitch)
                {
                    return "-" + Argument;
                }
                else
                {
                    return "--" + Argument;
                }
            }
            else
            {
                return Argument;
            }
        }

        public bool Equals(CommandArgument other) => IsSwitch == other.IsSwitch && Argument.Equals(other.Argument);
        public override bool Equals(object obj) => obj is CommandArgument other && Equals(other);
        public override int GetHashCode() => Argument.GetHashCode() << (IsSwitch ? 1 : 0);

        //OPERATORS
        public static bool operator ==(CommandArgument a, CommandArgument b) => a.Equals(b);
        public static bool operator !=(CommandArgument a, CommandArgument b) => !a.Equals(b);
        public static explicit operator string(CommandArgument a) => a.ToString();
    }
}
