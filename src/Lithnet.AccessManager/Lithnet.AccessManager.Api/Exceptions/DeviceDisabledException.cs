using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class DeviceDisabledException : DeviceAuthenticationException
    {
        public DeviceDisabledException() { }

        public DeviceDisabledException(string message) : base(message) { }

        public DeviceDisabledException(string message, Exception inner) : base(message, inner) { }

        protected DeviceDisabledException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
