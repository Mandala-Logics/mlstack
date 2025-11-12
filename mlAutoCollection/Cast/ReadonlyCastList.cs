using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace mlAutoCollection.Cast
{
    public sealed class ReadonlyCastList<TIn, TOut> : IReadOnlyList<TOut>
    {
        //PUBLIC PROPERTIES
        public TOut this[int index]
        {
            get
            {
                try { return convertor.Invoke(baseList[index]); }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }
        }
        public int Count => baseList.Count;

        //PRIVATE PROPERTIES
        private readonly IReadOnlyList<TIn> baseList;
        private readonly Func<TIn, TOut> convertor;

        //CONSTRCUTORS
        public ReadonlyCastList(IReadOnlyList<TIn> baseList, Func<TIn, TOut> convertor)
        {
            this.baseList = baseList ?? throw new ArgumentNullException(nameof(baseList));
            this.convertor = convertor ?? throw new ArgumentNullException(nameof(convertor));
        }

        //PUBLIC METHODS
        public IEnumerator<TOut> GetEnumerator() => new CastEnumerator<TIn, TOut>(baseList.GetEnumerator(), convertor);
        IEnumerator IEnumerable.GetEnumerator() => new CastEnumerator<TIn, TOut>(baseList.GetEnumerator(), convertor);
    }
}