using System;

namespace Lithnet.AccessManager.Server.Exceptions
{
    [Serializable]
    public class NoMatchingTargetException : AccessManagerException
    {
        public NoMatchingTargetException()
        {
        }

        public NoMatchingTargetException(string message)
            : base(message)
        {
        }

        public NoMatchingTargetException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NoMatchingTargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}