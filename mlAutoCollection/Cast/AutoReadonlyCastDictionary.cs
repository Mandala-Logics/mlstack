using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mlAutoCollection.Cast
{
    public sealed class AutoReadonlyCastDictionary<keyT, baseValT, castValT> : IReadOnlyDictionary<keyT, castValT> where baseValT : castValT
    {
        //PRIVATE METHODS
        private readonly IReadOnlyDictionary<keyT, baseValT> baseDict;

        //PUBLIC PROPERTIES
        public castValT this[keyT key] => baseDict[key];
        public IEnumerable<keyT> Keys => baseDict.Keys;
        public IEnumerable<castValT> Values => baseDict.Values.Cast<castValT>();
        public int Count => baseDict.Count;

        //CONSTRCUTORS
        public AutoReadonlyCastDictionary(IReadOnlyDictionary<keyT, baseValT> baseDict)
        {
            this.baseDict = baseDict ?? throw new ArgumentNullException(nameof(baseDict));
        }

        //PUBLIC METHODS
        public bool ContainsKey(keyT key) => baseDict.ContainsKey(key);
        public bool TryGetValue(keyT key, out castValT value)
        {
            var ret = baseDict.TryGetValue(key, out baseValT val);

            value = val;
            return ret;
        }
        public IEnumerator<KeyValuePair<keyT, castValT>> GetEnumerator() => new CastDictionaryEnumerator(baseDict.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => new CastDictionaryEnumerator(baseDict.GetEnumerator());

        //SUB CLASSES
        private sealed class CastDictionaryEnumerator : IEnumerator<KeyValuePair<keyT, castValT>>
        {
            //PUBLIC PROPERTIES
            public KeyValuePair<keyT, castValT> Current => new KeyValuePair<keyT, castValT>(baseEnum.Current.Key, baseEnum.Current.Value);
            object IEnumerator.Current => Current;

            //PRIVATE PROPERTIES
            private readonly IEnumerator<KeyValuePair<keyT, baseValT>> baseEnum;

            //CONSTRCUTORS
            public CastDictionaryEnumerator(IEnumerator<KeyValuePair<keyT, baseValT>> baseEnum)
            {
                this.baseEnum = baseEnum ?? throw new ArgumentNullException(nameof(baseEnum));
            }

            //PUBLIC PROPERTIES
            public void Dispose() { }
            public bool MoveNext() => baseEnum.MoveNext();
            public void Reset() => baseEnum.Reset();
        }
    }
}