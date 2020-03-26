using System;
using System.Runtime.Serialization;

namespace Senla.Core.Serialize
{
    [Serializable]
    internal class FrameSizeOutOfBoundException : Exception
    {
        public FrameSizeOutOfBoundException()
        {
        }

        public FrameSizeOutOfBoundException(string message) : base(message)
        {
        }

        public FrameSizeOutOfBoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FrameSizeOutOfBoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}