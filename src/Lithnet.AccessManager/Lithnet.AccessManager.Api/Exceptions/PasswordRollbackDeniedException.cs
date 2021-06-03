using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    [Serializable]
    public class PasswordRollbackDeniedException : AccessManagerException
    {
        public PasswordRollbackDeniedException() { }

        public PasswordRollbackDeniedException(string message) : base(message) { }

        public PasswordRollbackDeniedException(string message, Exception inner) : base(message, inner) { }

        protected PasswordRollbackDeniedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
