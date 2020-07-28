using System;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class CertificateIdentityNotFoundException : AccessManagerException
    {
        public CertificateIdentityNotFoundException()
        {
        }

        public CertificateIdentityNotFoundException(string message)
            : base(message)
        {
        }

        public CertificateIdentityNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CertificateIdentityNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}