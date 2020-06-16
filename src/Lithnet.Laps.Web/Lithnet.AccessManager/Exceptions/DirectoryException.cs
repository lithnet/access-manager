using System;
using System.Runtime.Serialization;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class DirectoryException : LithnetAccessManagerException
    {
        public DirectoryException()
        {
        }

        public DirectoryException(string message) : base(message)
        {
        }

        public DirectoryException(string message, Exception inner) : base(message, inner)
        {
        }

        public DirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}