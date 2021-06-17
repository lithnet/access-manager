using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class DeviceAuthenticationException : AccessManagerException
    {
        public DeviceAuthenticationException() { }

        public DeviceAuthenticationException(string message) : base(message) { }

        public DeviceAuthenticationException(string message, Exception inner) : base(message, inner) { }

        protected DeviceAuthenticationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
