using System;
using System.Runtime.Serialization;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class AccessManagerException : Exception
    {
        public AccessManagerException()
        {
        }

        public AccessManagerException(string message) : base(message)
        {
        }

        public AccessManagerException(string message, Exception inner) : base(message, inner)
        {
        }


        public AccessManagerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}