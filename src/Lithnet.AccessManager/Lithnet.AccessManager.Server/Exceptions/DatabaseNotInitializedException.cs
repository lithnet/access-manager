using System;

namespace Lithnet.AccessManager.Server
{
    public class DatabaseNotInitializedException : Exception
    {
        public DatabaseNotInitializedException() { }
        public DatabaseNotInitializedException(string message) : base(message) { }
        public DatabaseNotInitializedException(string message, Exception inner) : base(message, inner) { }
        protected DatabaseNotInitializedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
