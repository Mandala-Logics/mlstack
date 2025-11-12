using System;
using System.IO;

namespace mlCommand
{
    public sealed class CommandTree
    {
        //STATIC PROPERTIES
        private readonly static Func<string, int, CommandTreeNode> parseDeleagte = new Func<string, int, CommandTreeNode>((val, line) => new CommandTreeNode(val, line));

        //PUBLIC PROPERTIES


        //PRIVATE PROPERTIES
        private readonly ObjectTreeNode<CommandTreeNode> root;

        //CONSTRCUTORS
        public CommandTree(StreamReader reader)
        {
            root = ObjectTreeNode<CommandTreeNode>.ReadTree(reader, parseDeleagte);
        }

        //PUBLIC PROPERTIES
        public ParsedCommand ParseCommandLine(CommandLine line)
        {
            if ((root?.Count ?? 0) == 0) { throw new InvalidOperationException("Command tree is not definied."); }

            return root.Value.Parse(line, root);
        }
    }
}