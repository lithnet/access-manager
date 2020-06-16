using System;

namespace Lithnet.AccessManager.Web
{
    [Serializable]
    public class NoPasswordException : AccessManagerException
    {
        public NoPasswordException()
        {
        }

        public NoPasswordException(string message)
            : base(message)
        {
        }

        public NoPasswordException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NoPasswordException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}