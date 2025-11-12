using System;
using System.Collections.Generic;
using System.Text;

namespace DDEncoder
{
    public enum EncodingExceptionReason
    {
        Other, BadBinaryHeader, IncorrectValueCount, UnexpctedType, InvalidValue, FailedToReadStream, WrongIOMode, FailedToWriteStream, WrongImplimentation
    }

    public class EncodingException : Exception
    {   
        public EncodingExceptionReason Reason { get; }
        public EncodingException(string message) : base(message) { Reason = EncodingExceptionReason.Other; }
        public EncodingException(string message, Exception innerException) : base(message, innerException) { Reason = EncodingExceptionReason.Other; }
        public EncodingException(string message, EncodingExceptionReason reason) : base(message) { Reason = reason; }
        public EncodingException(string message, EncodingExceptionReason reason, Exception innerException) : base(message, innerException) { Reason = reason; }
        public EncodingException(EncodingExceptionReason reason) : base(GetMessage(reason)) { Reason = reason; }
        public EncodingException(EncodingExceptionReason reason, Exception innerException) : base(GetMessage(reason), innerException) { Reason = reason; }
        public static string GetMessage(EncodingExceptionReason reason)
        {
            switch (reason)
            {                
                case EncodingExceptionReason.BadBinaryHeader:
                    return "Object or value has bad binary header.";
                case EncodingExceptionReason.IncorrectValueCount:
                    return "Wrong number of values in object read.";
                case EncodingExceptionReason.UnexpctedType:
                    return "Object read unexpected value type from stream.";
                case EncodingExceptionReason.InvalidValue:
                    return "The value read from the stream appers to be invalid.";
                case EncodingExceptionReason.FailedToReadStream:
                    return "Failed to read enough bytes from stream.";
                case EncodingExceptionReason.WrongIOMode:
                    return "EncodedObject is not in the correct read/write mode as expected.";
                case EncodingExceptionReason.FailedToWriteStream:
                    return "Failed to write to stream.";
                case EncodingExceptionReason.WrongImplimentation:
                    return "Interface implimentation either doesn't have correct constructor or lacks valid ID.";
                case EncodingExceptionReason.Other:
                default:
                    return "Failed to read object or value.";
            }
        }
    }

    public class IFuckedUpException : Exception
    {
        public IFuckedUpException()
        {
        }

        public IFuckedUpException(string message) : base(message)
        {
        }

        public IFuckedUpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
