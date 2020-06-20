using System;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class CertificateNotFoundException : AccessManagerException
    {
        public CertificateNotFoundException()
        {
        }

        public CertificateNotFoundException(string message)
            : base(message)
        {
        }

        public CertificateNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CertificateNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}