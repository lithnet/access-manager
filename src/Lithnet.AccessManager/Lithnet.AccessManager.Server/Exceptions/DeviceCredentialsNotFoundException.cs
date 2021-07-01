using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class DeviceCredentialsNotFoundException : DeviceNotFoundException
    {
        public DeviceCredentialsNotFoundException() { }

        public DeviceCredentialsNotFoundException(string message) : base(message) { }

        public DeviceCredentialsNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected DeviceCredentialsNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
