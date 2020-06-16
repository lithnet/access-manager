using System;
using System.Runtime.Serialization;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class LithnetAccessManagerException : Exception
    {
        public LithnetAccessManagerException()
        {
        }

        public LithnetAccessManagerException(string message) : base(message)
        {
        }

        public LithnetAccessManagerException(string message, Exception inner) : base(message, inner)
        {
        }

        public LithnetAccessManagerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}