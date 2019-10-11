using System;

namespace Lithnet.Laps.Web
{
    [Serializable]
    public class AmbiguousNameException : Exception
    {
        public AmbiguousNameException()
        {
        }

        public AmbiguousNameException(string message)
            : base(message)
        {
        }

        public AmbiguousNameException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected AmbiguousNameException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}