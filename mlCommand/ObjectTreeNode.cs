using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace mlCommand
{
    public class ObjectTreeNode<T> : IReadOnlyList<ObjectTreeNode<T>>
    {
        //PUBLIC PROPERTIES
        public bool HasParent => parent is ObjectTreeNode<T>;
        public bool IsRoot => parent is null;
        public bool HasChildren => Count > 0;
        public ObjectTreeNode<T> Root
        {
            get
            {
                var n = this;
            
                while (n.HasParent)
                {
                    n = n.parent;
                }

                return n;
            }
        }
        public int Depth
        {
            get
            {
                var n = this;
                int ret = 0;
            
                while (n.HasParent)
                {
                    n = n.parent;
                    ret++;
                }

                return ret;
            }
        }
        public ObjectTreeNode<T> Parent
        {
            get
            {
                return parent ?? throw new InvalidOperationException("This is a root node and does not have a parent or siblings.");
            }
        }
        public int Count => children.Count;
        public T Value {get;}

        public ObjectTreeNode<T> this[int index] => children[index];
        public IReadOnlyList<ObjectTreeNode<T>> Children {get;}

        //PRIVATE PROPERTIES
        private readonly List<ObjectTreeNode<T>> children = new List<ObjectTreeNode<T>>();
        private readonly ObjectTreeNode<T> parent;

        //CONSTRUCTORS
        private protected ObjectTreeNode(ObjectTreeNode<T> parent, T value)
        {
            this.parent = parent;
            Value = value;

            Children = new ReadOnlyListWrapper<ObjectTreeNode<T>>(children);
        }

        //PRIVATE METHODS
        private string GetSerialString(int mod = 0)
        {
            var sb = new StringBuilder();

            for (int x = 0; x < Depth - mod; x++)
            {
                sb.Append("| ");
            }

            sb.Append(Value);

            return sb.ToString();
        }
        private void DoWriteNode(StreamWriter writer, int start)
        {
            writer.WriteLine(GetSerialString(start));

            foreach (var child in children)
            {
                child.DoWriteNode(writer, start);
            }
        }

        //PUBLIC METHODS
        public void WriteTree(StreamWriter writer)
        {
            if (writer is null) { throw new ArgumentNullException(nameof(writer)); }

            DoWriteNode(writer, Depth);
        }
        public ObjectTreeNode<T> AddChild(T value)
        {
            var child = new ObjectTreeNode<T>(this, value);

            children.Add(child);

            return child;
        }
        public ObjectTreeNode<T> AddSibling(T value) => parent.AddChild(value);
        public IEnumerator<ObjectTreeNode<T>> GetEnumerator() => children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();
        public override string ToString() => Value.ToString();

        //STATIC METHODS
        public static ObjectTreeNode<T> CreateRoot(T value)
        {
            return new ObjectTreeNode<T>(null, value);
        }
        public static ObjectTreeNode<T> ReadTree(StreamReader reader, Func<string, int, T> parseFunc)
        {
            if (reader is null) { throw new ArgumentNullException(nameof(reader)); }

            var match = new TreeNodeMatch(parseFunc, reader.ReadLine(), 1);

            var node = new ObjectTreeNode<T>(null, match.Value);
            var ret = node;
            int currDepth = match.Depth, diff, x;
            int n = 1;

            while (!reader.EndOfStream)
            {
                match = new TreeNodeMatch(parseFunc, reader.ReadLine(), ++n);

                diff = currDepth - match.Depth;

                if (diff == 0) //at same level
                {
                    if (node.IsRoot) { throw new IOException("Cannnot have two nodes which are both at 'root' level, line: " + n); }

                    node = node.Parent.AddChild(match.Value);
                }
                else if (diff == -1) //moving to children
                {
                    node = node.AddChild(match.Value);
                }
                else if (diff > 0) //going back up
                {
                    for (x = 0; x <= diff; x++)
                    {
                        node = node.parent;
                    }

                    node = node.AddChild(match.Value);
                }
                else { throw new IOException("Tree structue cannot jump in depth, bypassing creating children, line: " + n); } //diff is < -1

                currDepth = match.Depth;
            }

            return ret;
        }

        //NESTED STRCUTS
        private readonly struct TreeNodeMatch
        {
            //STATIC PROPERTIES
            private static readonly Regex lineRegex = new Regex(@"^((?<depth>\|\s)*)(?<value>\S+)\s*($|;|#)", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

            //PUBLIC PROPERTIES
            public int Depth {get;}
            public T Value {get;}
            public Match Match {get;}

            //CONSTRCUTORS
            public TreeNodeMatch(Func<string, int, T> parseFunc, string line, int n)
            {
                Match = lineRegex.Match(line);

                if (!Match.Success) { throw new IOException($"Line {n} does not apprear to be part of a StringTree structure."); }

                Depth = Match.Groups["depth"].Captures.Count;

                try
                {
                    Value = parseFunc.Invoke(Match.Groups[2].Value, n);
                }
                catch (TargetInvocationException e)
                {
                    throw new IOException($"Parse function failed on line {n}: " + e.InnerException.Message);
                }
            }
        }
    }
}