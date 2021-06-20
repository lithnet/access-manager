using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class AadTenantNotFoundException : AccessManagerException
    {
        public AadTenantNotFoundException() { }

        public AadTenantNotFoundException(string message) : base(message) { }

        public AadTenantNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected AadTenantNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
