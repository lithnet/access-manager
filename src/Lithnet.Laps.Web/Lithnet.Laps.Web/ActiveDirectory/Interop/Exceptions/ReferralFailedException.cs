using System;

namespace Lithnet.Laps.Web
{
    [Serializable]
    public class ReferralFailedException : DirectoryException
    {
        public ReferralFailedException()
        {
        }

        public ReferralFailedException(string message)
            : base(message)
        {
        }

        public ReferralFailedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ReferralFailedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}