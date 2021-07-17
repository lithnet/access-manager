using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class GroupNotFoundException : AccessManagerException
    {
        public GroupNotFoundException() { }

        public GroupNotFoundException(string message) : base(message) { }

        public GroupNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected GroupNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
