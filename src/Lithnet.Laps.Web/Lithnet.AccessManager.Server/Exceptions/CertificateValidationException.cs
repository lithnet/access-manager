using System;

namespace Lithnet.AccessManager.Server.Exceptions
{
    [Serializable]
    public class CertificateValidationException : AccessManagerException
    {
        public CertificateValidationException()
        {
        }

        public CertificateValidationException(string message)
            : base(message)
        {
        }

        public CertificateValidationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CertificateValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}