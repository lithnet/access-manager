using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class AadDeviceNotFoundException : DeviceAuthenticationException
    {
        public AadDeviceNotFoundException() { }

        public AadDeviceNotFoundException(string message) : base(message) { }

        public AadDeviceNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected AadDeviceNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
