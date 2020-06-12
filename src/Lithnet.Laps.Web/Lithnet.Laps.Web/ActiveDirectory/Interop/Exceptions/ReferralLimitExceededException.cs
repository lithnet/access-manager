using System;

namespace Lithnet.Laps.Web
{
    [Serializable]
    public class ReferralLimitExceededException : DirectoryException
    {
        public ReferralLimitExceededException()
        {
        }

        public ReferralLimitExceededException(string message)
            : base(message)
        {
        }

        public ReferralLimitExceededException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ReferralLimitExceededException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}