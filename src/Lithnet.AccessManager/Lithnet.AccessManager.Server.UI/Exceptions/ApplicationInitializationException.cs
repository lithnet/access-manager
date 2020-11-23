using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    [Serializable]
    public class ApplicationInitializationException : AccessManagerException
    {
        public ApplicationInitializationException() { }

        public ApplicationInitializationException(string message) : base(message) { }

        public ApplicationInitializationException(string message, Exception inner) : base(message, inner) { }

        protected ApplicationInitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
