using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class AssertionReplayException : AccessManagerException
    {
        public AssertionReplayException() { }

        public AssertionReplayException(string message) : base(message) { }

        public AssertionReplayException(string message, Exception inner) : base(message, inner) { }

        protected AssertionReplayException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
