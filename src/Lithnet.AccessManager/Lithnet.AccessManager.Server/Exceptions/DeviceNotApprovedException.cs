using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class DeviceNotApprovedException : AccessManagerException
    {
        public DeviceNotApprovedException() { }

        public DeviceNotApprovedException(string message) : base(message) { }

        public DeviceNotApprovedException(string message, Exception inner) : base(message, inner) { }

        protected DeviceNotApprovedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
