using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class CertificateNotFoundException : Exception
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

        protected CertificateNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
