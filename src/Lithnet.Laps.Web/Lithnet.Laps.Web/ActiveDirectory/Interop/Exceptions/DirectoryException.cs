using System;
using System.Runtime.Serialization;

namespace Lithnet.Laps.Web
{
    [Serializable]
    public class DirectoryException : LapsWebAppException
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