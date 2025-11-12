using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace mlCommand
{
    public enum CommandTreeNodeType { Null = 0, Switch, Argument, Command }
    public enum CommandTreeCountType { Null = 0, ZeroOrOne, ZeroOrMore, OneOrMore, FixedRange }

    public sealed class CommandTreeNode
    {
        //STATIC PROPERTIES
        private static readonly Regex lineRegex = new Regex(@"^(?<count>\*|\?|\+|(\d+-\d+|\d\+|\d))?\s*(?<greed>\?|\!+)?(?<type>\^|\$|\%)(?<val>\S+)\s*$", RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        //PUBLIC PROPERTIES
        public CommandTreeNodeType Type {get;}
        public bool IsSwitch => Type == CommandTreeNodeType.Switch;
        public bool IsArgument => Type == CommandTreeNodeType.Argument;
        public bool IsCommand => Type == CommandTreeNodeType.Command;
        public CommandTreeCountType CountType
        {
            get
            {
                if (!Length.HasMax)
                {
                    if (Length.Min == 0) { return CommandTreeCountType.ZeroOrMore; }
                    else { return CommandTreeCountType.OneOrMore; }
                }
                else
                {
                    if (Length.Max == 1 && Length.Min == 0) { return CommandTreeCountType.ZeroOrOne; }
                    else { return CommandTreeCountType.FixedRange; }
                }
            }
        }
        internal CommandRange Length {get;}
        public int Greed {get;}
        public string Line {get;}
        public IReadOnlyList<string> Switches
        {
            get
            {
                if (IsSwitch)
                {
                    return new ReadOnlyListWrapper<string>(switches);
                }
                else { throw new InvalidOperationException($"Cannot get switches for a CommandTreeNode of type {Type}."); }
            }
        }
        public string Argument
        {
            get
            {
                if (IsArgument)
                {
                    return switches[0];
                }
                else { throw new InvalidOperationException($"Cannot get argument for a CommandTreeNode of type {Type}."); }
            }
        }
        public string Command
        {
            get
            {
                if (IsCommand)
                {
                    return switches[0];
                }
                else { throw new InvalidOperationException($"Cannot get command for a CommandTreeNode of type {Type}."); }
            }
        }
        public string SwitchString
        {
            get
            {
                if (IsSwitch)
                {
                    return string.Join('+', switches);
                }
                else { throw new InvalidOperationException($"Cannot get switches for a CommandTreeNode of type {Type}."); }
            }
        }

        //PRIVATE PROPERTIES
        private readonly List<string> switches;

        //CONSTRCUTORS
        public CommandTreeNode(string val, int n)
        {
            Line = val;

            var match = lineRegex.Match(val);            

            if (!match.Success)
            {
                throw new WrongImplimentationException($"Line {n}: Unable to parse line '{val}'");
            }

            var g = match.Groups["count"];
            char c;

            if (g.Success)
            {
                c = g.Value[0];

                Length = c switch
                {
                    '?' => new CommandRange(0, 1),
                    '+' => new CommandRange(1, int.MaxValue),
                    '*' => new CommandRange(0, int.MaxValue),
                    _ => new CommandRange(g.Value, n),
                };
            }
            else
            {
                Length = new CommandRange(1, 1);
            }

            g = match.Groups["greed"];

            if (g.Success)
            {
                if (g.Value == "?")
                {
                    Greed = -1;
                }
                else //equals {n}!
                {
                    Greed = g.Value.Length;
                }
            }
            else
            {
                Greed = 0;
            }

            g = match.Groups["type"];

            c = g.Value[0];

            switch (c)
            {
                case '$':
                    Type = CommandTreeNodeType.Argument;
                    break;
                case '%':
                    Type = CommandTreeNodeType.Switch;
                    if (Length.Min > 1 || Length.Max > 1) { throw new WrongImplimentationException($"Line {n}: switches (%) may only be optional (?) or required (1), no other counts are valid."); }
                    break;
                case '^':
                    Type = CommandTreeNodeType.Command;
                    if (Greed != 0) { throw new WrongImplimentationException($"Line {n}: greed modifiers are not valid for commands."); }
                    break;
                default:
                    throw new Exception();
            }

            if (Type == CommandTreeNodeType.Switch)
            {   
                switches = new List<string>(match.Groups["val"].Value.Split('+'));
            }
            else
            {
                switches = new List<string>() { match.Groups["val"].Value };
            }            
        }

        //PUBLIC METHODS
        public ParsedCommand Parse(IEnumerable<CommandArgument> args, ObjectTreeNode<CommandTreeNode> node)
        {
            ParsedCommand? retNested = null;
            Dictionary<string, IEnumerable<string>> retArgs = new Dictionary<string, IEnumerable<string>>();
            List<ParsedSwitch> retSwitches = new List<ParsedSwitch>();

            List<string> ret;

            List<ParsedRange> ranges;

            ObjectTreeNode<CommandTreeNode> curr = null;
            IEnumerable<CommandArgument> myArgs;
            IEnumerable<ObjectTreeNode<CommandTreeNode>> myNodes;

            IEnumerable<CommandArgument> sectionArgs = args.TakeAfter((a) =>
                node.Any((n) => n.Value.IsCommand && n.Value.Command.Equals(a.Argument)));

            if (sectionArgs.Any())
            {
                curr = node.First((n) => n.Value.IsCommand && n.Value.Command.Equals(sectionArgs.First().Argument));

                retNested = curr.Value.Parse(sectionArgs.Skip(1), curr);

                myArgs = args.TakeBefore((a) => a.Command.Equals(sectionArgs.First().Command)).ToArray();

                myNodes = node.Children.TakeBefore((n) => n.Value.switches[0].Equals(sectionArgs.First().Argument)).ToArray();

                if (!myNodes.Any() && !myNodes.Any()) { return new ParsedCommand(node.Value.Command, null, null, retNested); }
            }
            else
            {
                myArgs = args;
                myNodes = node.Children;
            }

            sectionArgs = myArgs.TakeWhile((a) => a.IsArgument);
            IEnumerable<ObjectTreeNode<CommandTreeNode>> sectionNodes = myNodes.TakeWhile((n) => n.Value.IsArgument);
            int argCount = sectionArgs.Count();
            int minCount = TotalMinCount(sectionNodes);
            int r, x, c = 0;

            if (minCount > argCount)
            {
                if (argCount > 0)
                {
                    throw new CommandParsingException($"Expected more arguments after '{sectionArgs.Last()}', at least {minCount - argCount} more, see help.");
                }
                else
                {
                    throw new CommandParsingException($"Expected arguments after '{node.Value.Command}', at least {minCount}, see help.");
                }
            }

            if (argCount > 0)
            {
                if (sectionNodes.Any())
                {
                    ranges = DistributeArgs(argCount, sectionNodes);

                    if (TotalParsedLength(ranges) < argCount)
                    { throw new CommandParsingException("Unexpected argument(s): " + string.Join(' ', sectionArgs.TakeLast(argCount - TotalParsedLength(ranges)))); }
                    else
                    {
                        IEnumerator<CommandArgument> a = sectionArgs.GetEnumerator();
                        IEnumerator<ObjectTreeNode<CommandTreeNode>> n = sectionNodes.GetEnumerator();

                        for (r = 0; r < ranges.Count; r++)
                        {
                            ret = new List<string>(ranges[r].Length);

                            n.MoveNext();

                            for (x = 0; x < ranges[r].Length; x++)
                            {
                                a.MoveNext();
                                c++;

                                ret.Add(a.Current.Argument);
                            }

                            if (n.Current.Value.Length.Min > ret.Count)
                            {
                                throw new CommandParsingException($"Not enough parameters given for the argument '{n.Current.Value.Argument}', see help.");
                            }

                            if (ret.Count > 0) { retArgs.Add(n.Current.Value.Argument, ret); }
                        }

                        while (n.MoveNext())
                        {
                            if (n.Current.Value.Length.Min > 0)
                            {
                                throw new CommandParsingException($"The required argument '{n.Current.Value.Argument}' was not provided, see help.");
                            }
                        }

                        n.Dispose();
                        a.Dispose();
                    }
                }
                else if (!myNodes.First().Value.IsSwitch)
                {
                    throw new CommandParsingException("Unexpected leading arguments: " + string.Join(' ', sectionArgs));
                }
            }

            sectionArgs = myArgs.Skip(c);
            sectionNodes = myNodes.Where((n) => n.Value.IsSwitch);
            argCount = sectionArgs.Count();

            if (argCount > 0) //must start with a switch, if any present
            {
                IEnumerable<CommandArgument> switchArgs;
                c = 0;

                IEnumerator<CommandArgument> a = sectionArgs.GetEnumerator();

                while (a.MoveNext())
                {
                    c++;

                    if (a.Current.IsSwitch)
                    {
                        curr = sectionNodes.FirstOrDefault((n) => n.Value.Switches.Contains(a.Current.Switch));

                        Dictionary<string, IEnumerable<string>> tmp;

                        if (curr is null) { throw new CommandParsingException($"Unknown switch: '{a.Current}'"); }

                        int maxCount = TotalMaxCount(curr.Children);
                        minCount = TotalMinCount(curr.Children);

                        switchArgs = sectionArgs.Skip(c).TakeBefore((a) => a.IsSwitch);

                        if (maxCount < int.MaxValue && !sectionArgs.Skip(c).Any((n) => n.IsSwitch)) { switchArgs = switchArgs.Take(maxCount); }

                        argCount = switchArgs.Count();

                        if (minCount > argCount) { throw new CommandParsingException($"Expected at least {minCount} arguments after switch '{a.Current}'."); }

                        if (argCount == 0)
                        {
                            retSwitches.Add(new ParsedSwitch(curr.Value.switches[0]));
                        }
                        else if (!curr.HasChildren)
                        {
                            throw new CommandParsingException($"Unexpected argument(s) following switch '{a.Current.Switch}': {string.Join(' ', switchArgs)}");
                        }
                        else
                        {
                            ranges = DistributeArgs(argCount, curr.Children);
                            IEnumerator<ObjectTreeNode<CommandTreeNode>> n = curr.Children.GetEnumerator();

                            if (TotalParsedLength(ranges) < argCount)
                            { throw new CommandParsingException($"Unexpected argument(s) following switch '{a.Current.Switch}': {string.Join(' ', sectionArgs.TakeLast(argCount - TotalParsedLength(ranges)))}"); }

                            tmp = new Dictionary<string, IEnumerable<string>>();

                            for (r = 0; r < ranges.Count; r++)
                            {
                                ret = new List<string>(ranges[r].Length);

                                n.MoveNext();

                                if (!n.Current.Value.IsArgument) { throw new WrongImplimentationException($"Only arguments may be the children of switches: '{a.Current.Switch}'."); }

                                for (x = 0; x < ranges[r].Length; x++)
                                {
                                    a.MoveNext();
                                    c++;

                                    ret.Add(a.Current.Argument);
                                }

                                if (n.Current.Value.Length.Min > ret.Count)
                                {
                                    throw new CommandParsingException($"There are not enough arguments given follow the switch '{a.Current.Switch}', see help.");
                                }

                                if (ret.Count > 0) { tmp.Add(n.Current.Value.switches[0], ret); }
                            }

                            while (n.MoveNext())
                            {
                                if (!n.Current.Value.IsArgument) { throw new WrongImplimentationException($"Only arguments may be the children of switches: '{a.Current.Switch}'."); }

                                if (n.Current.Value.Length.Min > 0)
                                {
                                    throw new CommandParsingException($"There are not enough arguments given follow the switch '{a.Current.Switch}', see help.");
                                }
                            }

                            retSwitches.Add(new ParsedSwitch(curr.Value.switches[0], tmp));

                            n.Dispose();
                        }
                    }
                    else //hit non-consumed argument
                    {
                        sectionNodes = myNodes.Reverse().TakeWhile((n) => n.Value.IsArgument).Reverse();
                        sectionArgs = sectionArgs.Skip(c - 1);
                        argCount = sectionArgs.Count();
                        minCount = TotalMinCount(sectionNodes);

                        ranges = DistributeArgs(argCount, sectionNodes);

                        if (minCount > argCount)
                        {
                            if (argCount > 0)
                            {
                                throw new CommandParsingException($"Expected more arguments after '{node.Value.Command}', at least {minCount - argCount} more, see help.");
                            }
                            else
                            {
                                throw new CommandParsingException($"Expected arguments at end of command line, at least {minCount}, see help.");
                            }
                        }

                        if (sectionNodes.Any())
                        {
                            ranges = DistributeArgs(argCount, sectionNodes);

                            if (TotalParsedLength(ranges) < argCount)
                            { throw new CommandParsingException("Unexpected argument(s): " + string.Join(' ', sectionArgs.TakeLast(argCount - TotalParsedLength(ranges)))); }
                            else
                            {
                                IEnumerator<ObjectTreeNode<CommandTreeNode>> n = sectionNodes.GetEnumerator();

                                for (r = 0; r < ranges.Count; r++)
                                {
                                    ret = new List<string>(ranges[r].Length);

                                    n.MoveNext();

                                    for (x = 0; x < ranges[r].Length; x++)
                                    {
                                        ret.Add(a.Current.Argument);

                                        a.MoveNext();
                                    }

                                    if (n.Current.Value.Length.Min > ret.Count)
                                    {
                                        throw new CommandParsingException($"Not enough parameters given for the argument '{n.Current.Value.Argument}', see help.");
                                    }

                                    if (ret.Count > 0) { retArgs.Add(n.Current.Value.Argument, ret); }
                                }

                                while (n.MoveNext())
                                {
                                    if (n.Current.Value.Length.Min > 0)
                                    {
                                        throw new CommandParsingException($"The required argument '{n.Current.Value.Argument}' was not provided, see help.");
                                    }
                                }

                                n.Dispose();
                            }
                        }
                        else
                        {
                            throw new CommandParsingException("Hit unexpected argument: " + sectionArgs.First());
                        }

                        a.Dispose();
                    }
                }
            }

            var parsedCommand = new ParsedCommand(node.Value.Command, retArgs, retSwitches, retNested);

            var totalCount = retArgs.Count() + (retNested is null ? 0 : 1) + retSwitches.Count;

            if (node.Value.Length.Min > totalCount)
            {
                throw new CommandParsingException("Not enough commands or arguments following " + node.Value.Command);
            }

            return parsedCommand;
        }

        internal static List<ParsedRange> DistributeArgs(int count, IEnumerable<ObjectTreeNode<CommandTreeNode>> nodes)
        {
            int a, b, c;
            bool changed;
            List<ParsedRange> ranges = new List<ParsedRange>();

            if (!nodes.Any()) { return ranges; }

            b = 0;

            foreach (var node in nodes)
            {
                ranges.Add(new ParsedRange(b, c = node.Value.Length.Min, node.Value.Length.Max, node.Value.Greed));

                b += c;
            }

            do
            {
                changed = false;

                for (a = 0; a < ranges.Count - 1; a++)
                {
                    if (!ranges[a].AtMax)
                    {
                        if (ranges[a].End < ranges[a + 1].Start)
                        {
                            ranges[a].Streach(1, count);
                            changed = true;
                        }
                        else if (ranges[a].End == ranges[a + 1].Start && ranges[a].Greed >= ranges[a + 1].Greed)
                        {
                            for (b = ranges.Count - 1; b > a; b--)
                            {
                                if (!ranges[b].Shove(1, count)) { break; }
                                else { changed = true; }
                            }

                            if (changed) { ranges[a].Streach(1, count); }
                        }
                        else if (ranges[a].Length > 0 && ranges[a].End == ranges[a + 1].Start && ranges[a].Greed == -1 && ranges[a + 1].Greed > -1)
                        {
                            if (ranges[a].Streach(-1, count))
                            {
                                changed = true;
                                ranges[a + 1].Shove(-1, count);
                                ranges[a + 1].Streach(1, count);
                            }
                        }
                        else
                        {
                            if (ranges[a].Streach(1, count)) { changed = true; }
                        }
                    }
                }

                if (!ranges[^1].AtMax)
                {
                    if (ranges[^1].Streach(1, count)) { changed = true; }
                }

            } while (changed);

            return ranges;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();

            switch (CountType)
            {
                case CommandTreeCountType.ZeroOrOne:
                    sb.Append('?');
                    break;
                case CommandTreeCountType.ZeroOrMore:
                    sb.Append('*');
                    break;
                case CommandTreeCountType.OneOrMore:
                    sb.Append('+');
                    break;
                case CommandTreeCountType.FixedRange:
                    if (Length.Min == Length.Max) { sb.Append(Length.Min); }
                    else { sb.Append(string.Join('-', Length.Min, Length.Max)); }
                    break;
                default:
                    throw new Exception();
            }

            switch (Greed)
            {
                case -1:
                    sb.Append('?');
                    break;
                case 0:
                    break;
                default:
                    sb.Append(new string('!', Greed));
                    break;
            }

            switch (Type)
            {
                case CommandTreeNodeType.Switch:
                    sb.Append('%');
                    break;
                case CommandTreeNodeType.Argument:
                    sb.Append('$');
                    break;
                case CommandTreeNodeType.Command:
                    sb.Append('^');
                    break;
                default:
                    throw new Exception();
            }

            sb.Append(string.Join('+', switches));

            return sb.ToString();
        }

        //STATIC METHODS        
        private static int TotalMinCount(IEnumerable<ObjectTreeNode<CommandTreeNode>> ls)
        {
            int ret = 0;

            foreach (var x in ls) { ret += x.Value.Length.Min; }

            return ret;
        }

        private static int TotalParsedLength(IEnumerable<ParsedRange> ls)
        {
            int ret = 0;

            foreach (var x in ls) { ret += x.Length; }

            return ret;
        }

        private static int TotalMaxCount(IEnumerable<ObjectTreeNode<CommandTreeNode>> ls)
        {
            int ret = 0;

            foreach (var x in ls)
            { 
                if (x.Value.Length.HasMax)
                {
                    ret += x.Value.Length.Min; 
                }
                else
                {
                    return int.MaxValue;
                }                
            }

            return ret;
        }
    }
}