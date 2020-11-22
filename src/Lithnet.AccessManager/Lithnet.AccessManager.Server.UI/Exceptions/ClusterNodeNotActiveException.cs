using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    [Serializable]
    public class ClusterNodeNotActiveException : AccessManagerException
    {
        public ClusterNodeNotActiveException() { }
        public ClusterNodeNotActiveException(string message) : base(message) { }
        public ClusterNodeNotActiveException(string message, Exception inner) : base(message, inner) { }
        protected ClusterNodeNotActiveException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
