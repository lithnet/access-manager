using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class AuthorizationRuleImportException : Exception
    {
        public AuthorizationRuleImportException() { }

        public AuthorizationRuleImportException(string message) : base(message) { }
        
        public AuthorizationRuleImportException(string message, Exception inner) : base(message, inner) { }

        protected AuthorizationRuleImportException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
