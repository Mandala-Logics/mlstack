using System;
using System.Collections.Generic;
using System.Linq;

namespace mlCommand
{
    public static class LINQExtensions
    {
        public static IEnumerable<T> TakeAfter<T>(this IEnumerable<T> ls, Func<T, bool> predicate)
        {
            return ls.SkipWhile((x) => !predicate(x));
        }

        public static IEnumerable<T> TakeBefore<T>(this IEnumerable<T> ls, Func<T, bool> predicate)
        {
            bool take = true;

            return ls.Where((x) =>
            {
                if (predicate(x)) { take = false; }

                return take;

            });
        }
    }
}