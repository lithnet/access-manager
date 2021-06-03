using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class AssertionMissingNonceException : AccessManagerException
    {
        public AssertionMissingNonceException() { }

        public AssertionMissingNonceException(string message) : base(message) { }

        public AssertionMissingNonceException(string message, Exception inner) : base(message, inner) { }

        protected AssertionMissingNonceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
