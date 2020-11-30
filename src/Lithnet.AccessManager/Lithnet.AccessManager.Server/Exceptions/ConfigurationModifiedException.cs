using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    [Serializable]
    public class ConfigurationModifiedException : AccessManagerException
    {
        public ConfigurationModifiedException() { }

        public ConfigurationModifiedException(string message) : base(message) { }

        public ConfigurationModifiedException(string message, Exception inner) : base(message, inner) { }

        protected ConfigurationModifiedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
