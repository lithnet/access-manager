using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class AadObjectNotFoundException : AccessManagerException
    {
        public AadObjectNotFoundException() { }

        public AadObjectNotFoundException(string message) : base(message) { }

        public AadObjectNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected AadObjectNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
