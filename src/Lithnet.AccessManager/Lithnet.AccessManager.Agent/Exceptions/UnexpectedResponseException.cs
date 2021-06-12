using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class UnexpectedResponseException : AccessManagerException
    {
        public UnexpectedResponseException()
        {
        }

        public UnexpectedResponseException(string message)
            : base(message)
        {
        }

        public UnexpectedResponseException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnexpectedResponseException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
