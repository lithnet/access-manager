using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class DeviceNotFoundException : DeviceAuthenticationException
    {
        public DeviceNotFoundException() { }

        public DeviceNotFoundException(string message) : base(message) { }

        public DeviceNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected DeviceNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
