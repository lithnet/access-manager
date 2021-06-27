using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class AadMissingPermissionException : AccessManagerException
    {
        public IEnumerable<string> MissingPermissions { get; }

        public AadMissingPermissionException() { }

        public AadMissingPermissionException(string message, IEnumerable<string> missingPermissions)
            : base(message)
        {
            this.MissingPermissions = missingPermissions;
        }

        public AadMissingPermissionException(string message, Exception inner) : base(message, inner) { }

        protected AadMissingPermissionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
