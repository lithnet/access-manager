using System;

namespace Lithnet.Laps.Web
{
    [System.Serializable]
    public class NoMatchingTargetException : Exception
    {
        public NoMatchingTargetException()
        {
        }

        public NoMatchingTargetException(string message)
            : base(message)
        {
        }

        public NoMatchingTargetException(string message, Exception inner)
            : base(message, inner)
        {
        }


        protected NoMatchingTargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}