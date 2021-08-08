using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class MissingDependencyException : Exception
    {
        public MissingDependencyException()
        {
        }

        public MissingDependencyException(string message)
            : base(message)
        {
        }

        public MissingDependencyException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected MissingDependencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
