using System;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    [Serializable]
    public class ImportException : Exception
    {
        public ImportException() { }

        public ImportException(string message) : base(message) { }
        
        public ImportException(string message, Exception inner) : base(message, inner) { }

        protected ImportException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
