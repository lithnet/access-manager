using System;

namespace Lithnet.AccessManager.Web
{
    [Serializable]
    public abstract class LapsWebAppException : Exception
    {
        public LapsWebAppException()
        {
        }

        public LapsWebAppException(string message)
            : base(message)
        {
        }

        protected LapsWebAppException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        protected LapsWebAppException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected LapsWebAppException(Exception innerException)
            : base(null, innerException)
        {
        }
    }
}