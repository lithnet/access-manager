using System;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class AmbiguousNameException : DirectoryException
    {
        public AmbiguousNameException()
        {
        }

        public AmbiguousNameException(string message)
            : base(message)
        {
        }

        public AmbiguousNameException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected AmbiguousNameException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}