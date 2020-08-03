using System;

namespace Lithnet.AccessManager.Server.Exceptions
{
    [Serializable]
    public class InvalidIssuerException : AccessManagerException
    {
        public InvalidIssuerException()
        {
        }

        public InvalidIssuerException(string message)
            : base(message)
        {
        }

        public InvalidIssuerException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidIssuerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}