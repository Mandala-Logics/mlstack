using System;
using System.Collections;
using System.Collections.Generic;

namespace mlAutoCollection.Cast
{
    public sealed class CastEnumerable<TIn, TOut> : IEnumerable<TOut>
    {
        private readonly IEnumerable<TIn> baseEnum;
        private readonly Func<TIn, TOut> convertor;

        public CastEnumerable(IEnumerable<TIn> baseEnum, Func<TIn, TOut> convertor)
        {
            this.baseEnum = baseEnum ?? throw new ArgumentNullException(nameof(baseEnum));
            this.convertor = convertor ?? throw new ArgumentNullException(nameof(convertor));
        }

        public IEnumerator<TOut> GetEnumerator() => new CastEnumerator<TIn, TOut>(baseEnum.GetEnumerator(), convertor);

        IEnumerator IEnumerable.GetEnumerator() => new CastEnumerator<TIn, TOut>(baseEnum.GetEnumerator(), convertor);
    }
}