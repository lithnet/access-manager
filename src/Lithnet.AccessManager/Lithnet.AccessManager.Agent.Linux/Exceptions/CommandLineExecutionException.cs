using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class CommandLineExecutionException : Exception
    {
        public CommandLineExecutionException()
        {
        }

        public CommandLineExecutionException(string message)
            : base(message)
        {
        }

        public CommandLineExecutionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CommandLineExecutionException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
