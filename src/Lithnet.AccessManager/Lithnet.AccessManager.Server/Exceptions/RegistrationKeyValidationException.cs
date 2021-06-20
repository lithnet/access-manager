using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class RegistrationKeyValidationException : AccessManagerException
    {
        public RegistrationKeyValidationException() { }

        public RegistrationKeyValidationException(string message) : base(message) { }

        public RegistrationKeyValidationException(string message, Exception inner) : base(message, inner) { }

        protected RegistrationKeyValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
