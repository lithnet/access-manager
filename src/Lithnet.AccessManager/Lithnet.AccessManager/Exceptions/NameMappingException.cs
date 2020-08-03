using System;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class NameMappingException : DirectoryException
    {
        public NameMappingException()
        {
        }

        public NameMappingException(string message)
            : base(message)
        {
        }

        public NameMappingException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NameMappingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}