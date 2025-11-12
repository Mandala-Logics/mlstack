using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace mlAutoCollection.Cast
{
    public sealed class CastEnumerator<TIn, TOut> : IEnumerator<TOut>
    {
        public TOut Current
        {
            get
            {
                try { return convertor.Invoke(iEnum.Current); }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }
        }

        object IEnumerator.Current => Current;
        private readonly IEnumerator<TIn> iEnum;
        private readonly Func<TIn, TOut> convertor;

        //CONSTRCUTORS
        public CastEnumerator(IEnumerator<TIn> iEnum, Func<TIn, TOut> convertor)
        {
            this.iEnum = iEnum ?? throw new ArgumentNullException(nameof(iEnum));
            this.convertor = convertor ?? throw new ArgumentNullException(nameof(convertor));
        }

        //PUBLIC FUNCTIONS
        public void Dispose() => iEnum.Dispose();
        public bool MoveNext() => iEnum.MoveNext();
        public void Reset() => iEnum.Reset();
    }
}