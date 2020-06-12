using System;

namespace Lithnet.Laps.Web
{
    [Serializable]
    public class UnsupportedPrincipalTypeException : DirectoryException
    {
        public UnsupportedPrincipalTypeException()
        {
        }

        public UnsupportedPrincipalTypeException(string message)
            : base(message)
        {
        }

        public UnsupportedPrincipalTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnsupportedPrincipalTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}