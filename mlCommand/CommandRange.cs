using System.IO;

namespace mlCommand
{
    internal readonly struct CommandRange
    {
        public int Min {get;}
        public int Max {get;}
        public bool HasMax => Max < int.MaxValue;
        public bool FixedLength => HasMax; 

        public CommandRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public CommandRange(string range, int n)
        {
            int max, min;

            if (int.TryParse(range, out min))
            {
                Min = Max = min;
            }
            else if (range[^1] == '+')
            {
                Min = int.Parse(range[..^1]);

                Max = int.MaxValue;
            }
            else
            {
                var s = range.Split('-');

                if (s.Length == 2 && int.TryParse(s[0], out min) && int.TryParse(s[1], out max))
                {
                    if (min > max) { throw new WrongImplimentationException($"Line {n}: invalid range, min greater than max: '{range}'"); }

                    Min = min;
                    Max = max;
                }
                else
                {
                    throw new WrongImplimentationException($"Failed to parse count on line {n}: '{range}'");
                }
            }

            
        }
    }

    internal sealed class ParsedRange
    {
        //PUBLIC PROPERTIES
        public int Start {get; private set;}
        public int End {get; private set;}
        public int Length => End - Start;
        public int Min {get;}
        public int Max {get;}
        public bool AtMax => Max == Length;
        public int Greed {get;}

        //CONSTRCUTORS
        public ParsedRange(int start, int min, int max, int greed)
        {
            Start = start;
            End = start + min;
            Min = min;
            Max = max;
            Greed = greed;
        }   

        //PUBLIC PROPERTIES
        public bool Shove(int amt, int count)
        {
            int start = Start, end = End;
            int x = 0;

            Start += amt;
            End += amt;

            if (End > count) { x = count - End; End = count; }

            Start -= x;

            return Start != start || End != end;
        }
        public bool Streach(int amt, int count)
        {
            int prev = End;
            int x = End += amt;
            int len = x - Start;

            if (len < Min) { End = Start + Min; }
            else if (len > Max) { End = Start + Max; }
            else if (x < 0) { End = 0; }
            else if (x > count) { End = count; }
            else { End = x; }   

            return prev != End;       
        } 
    }
}