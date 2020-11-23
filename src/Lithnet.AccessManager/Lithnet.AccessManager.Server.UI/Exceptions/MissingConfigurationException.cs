using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    [Serializable]
    public class MissingConfigurationException : ApplicationInitializationException
    {
        public MissingConfigurationException() { }

        public MissingConfigurationException(string message) : base(message) { }

        public MissingConfigurationException(string message, Exception inner) : base(message, inner) { }

        protected MissingConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
