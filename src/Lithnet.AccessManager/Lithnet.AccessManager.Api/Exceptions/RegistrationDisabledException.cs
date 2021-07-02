using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class RegistrationDisabledException : AccessManagerException
    {
        public RegistrationDisabledException() { }

        public RegistrationDisabledException(string message) : base(message) { }

        public RegistrationDisabledException(string message, Exception inner) : base(message, inner) { }

        protected RegistrationDisabledException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
