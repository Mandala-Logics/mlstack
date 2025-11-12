using System;
using System.Collections.Generic;
using System.Linq;

namespace mlCommand
{
    public readonly struct ParsedCommand
    {
        //PUBLIC PROPERTIES
        public string Command {get;}
        public int TotalArgumentCount
        {
            get
            {  
                if (args is null) { return 0; }

                int x = 0;

                foreach (var kvp in args)
                {
                    x += kvp.Value.Count();
                }

                return x;
            }
        }
        public int ArgumentCount => args?.Count ?? 0;
        public int SwitchCount => switches?.Count() ?? 0;
        public bool HasNested => nested is ParsedCommand;
        public ParsedCommand Nested
        {
            get
            {
                if (nested is null) { throw new InvalidOperationException("Command does not contain a nested command."); }
                else { return (ParsedCommand)nested; }
            }
        }

        //PRIVATE PROPERTIES
        private readonly Dictionary<string, IEnumerable<string>> args;
        private readonly IEnumerable<ParsedSwitch> switches;
        private readonly object nested;
        
        //CONSTRCUTORS
        public ParsedCommand(string command, Dictionary<string, IEnumerable<string>> args, IEnumerable<ParsedSwitch> switches, ParsedCommand? nested)
        {
            Command = command;

            this.args = (args?.Any() ?? false) ? args : null;
            this.switches = (switches?.Any() ?? false) ? switches : null;
            this.nested = nested;
        }

        //PUBLIC METHODS
        public IEnumerable<string> GetArgumentValue(string argument) => args?[argument] ?? new string[0];
        public bool TryGetArgumentValue(string argument, out IEnumerable<string> val)
        {
            try
            {
                val = args?[argument] ?? throw new KeyNotFoundException();
                return true;
            }
            catch (KeyNotFoundException)
            {
                val = default;
                return false;
            }
        }
        public string JoinArgument(string argument) => string.Join(", ", GetArgumentValue(argument));
        public bool HasArgument(string arg)
        {
            return TryGetArgumentValue(arg, out _);
        }
        public bool HasSwitch(string sw)
        {
            if (switches is IEnumerable<ParsedSwitch>)
            {
                try { switches.First((ps) => ps.Switch.Equals(sw)); }
                catch (InvalidOperationException) { return false; }

                return true;
            }
            else
            {
                return false;
            }            
        }        
        public ParsedSwitch GetSwitch(string sw)
        {
            if (switches is IEnumerable<ParsedSwitch>)
            {
                return switches.First((ps) => ps.Switch.Equals(sw));
            }
            else
            {
                throw new InvalidOperationException("ParsedCommand does not contain any switches.");
            }   
        }
        public bool TryGetSwitch(string sw, out ParsedSwitch parsedSwitch)
        {
            try
            {
                parsedSwitch = switches?.First((ps) => ps.Switch.Equals(sw)) ?? throw new InvalidOperationException();
                return true;
            }
            catch (InvalidOperationException)
            {
                parsedSwitch = default;
                return false;
            }
        }
    }

    public readonly struct ParsedSwitch
    {
        //PUBLIC PROPERTIES
        public string Switch {get;}
        public int ArgumentCount => args?.Count ?? 0;

        //PRIVATE PROPERTIES
        private readonly Dictionary<string, IEnumerable<string>> args;

        //CONSTRCUTORS
        public ParsedSwitch(string s, Dictionary<string, IEnumerable<string>> args)
        {
            Switch = s;
            this.args = (args?.Any() ?? false) ? args : null;
        }
        public ParsedSwitch(string s)
        {
            Switch = s;
            args = null;
        }

        //PUBLIC METHODS
        public IEnumerable<string> GetArgumentValue(string argument) => args?[argument] ?? new string[0];
        public bool TryGetArgumentValue(string argument, out IEnumerable<string> val)
        {
            try
            {
                val = args?[argument] ?? throw new KeyNotFoundException();
                return true;
            }
            catch (KeyNotFoundException)
            {
                val = default;
                return false;
            }
        }
        public string JoinArgument(string argument) => string.Join(", ", GetArgumentValue(argument));
    }
}