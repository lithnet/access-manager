using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class ComputerNotAadJoinedException : AccessManagerException
    {
        public ComputerNotAadJoinedException()
        {
        }

        public ComputerNotAadJoinedException(string message)
            : base(message)
        {
        }

        public ComputerNotAadJoinedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ComputerNotAadJoinedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
