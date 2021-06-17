using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class UnsupportedAuthenticationTypeException : DeviceAuthenticationException
    {
        public UnsupportedAuthenticationTypeException() { }

        public UnsupportedAuthenticationTypeException(string message) : base(message) { }

        public UnsupportedAuthenticationTypeException(string message, Exception inner) : base(message, inner) { }

        protected UnsupportedAuthenticationTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
