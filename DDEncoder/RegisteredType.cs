using System;
using System.Linq;
using System.Reflection;

namespace DDEncoder
{
    public readonly struct RegisteredType
    {
        public static readonly Type InterfaceType = typeof(IEncodable);
        private static readonly Type[] constrcutorTypes = new Type[] { typeof(EncodedObject)};

        public Type Type { get; }
        public ConstructorInfo Constructor { get; }

        public RegisteredType(Type type)
        {
            Type = type ?? throw new ArgumentNullException("type");

            if (type.IsAbstract) throw new EncodingException($"Abstract classes cannot be registered for decoding/encoding, {type.FullName} is abstract.", EncodingExceptionReason.WrongImplimentation);
            if (!type.GetInterfaces().Contains(InterfaceType)) throw new EncodingException($"Only types which implement IEncodaled may be registered for decoding, {type.FullName} does not.", EncodingExceptionReason.WrongImplimentation);

            Constructor = type.GetConstructor(constrcutorTypes);

            if (Constructor is null) throw new EncodingException($"Types implementing IEncodable must have a constructor with argument of type EncodedObject, {type.FullName} does not.", EncodingExceptionReason.WrongImplimentation);
        }

        public IEncodable Construct(EncodedObject encodedObject)
        {
            if (encodedObject is null) throw new ArgumentNullException("encodedObject");

            IEncodable ret;
            
            try { ret = (IEncodable)Constructor.Invoke(new object[] { encodedObject }); }
            catch (TargetInvocationException e) { throw e.InnerException; }

            if (ret is null) throw new EncodingException($"Failed to contruct type {Type}; constructor returned null.");

            return ret;
        }
    }
}
