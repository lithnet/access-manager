using System;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class TooManyResultsException : DirectoryException
    {
        public TooManyResultsException()
        {
        }

        public TooManyResultsException(string message)
            : base(message)
        {
        }

        public TooManyResultsException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TooManyResultsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}