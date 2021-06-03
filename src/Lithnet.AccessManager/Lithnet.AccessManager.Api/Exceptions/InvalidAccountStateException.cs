using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class InvalidAccountStateException : AccessManagerException
    {
        public InvalidAccountStateException() { }

        public InvalidAccountStateException(string message) : base(message) { }

        public InvalidAccountStateException(string message, Exception inner) : base(message, inner) { }

        protected InvalidAccountStateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
