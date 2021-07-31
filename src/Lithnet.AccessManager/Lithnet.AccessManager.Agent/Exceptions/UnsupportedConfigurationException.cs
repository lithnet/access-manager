using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class UnsupportedConfigurationException : Exception
    {
        public UnsupportedConfigurationException()
        {
        }

        public UnsupportedConfigurationException(string message)
            : base(message)
        {
        }

        public UnsupportedConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected UnsupportedConfigurationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
