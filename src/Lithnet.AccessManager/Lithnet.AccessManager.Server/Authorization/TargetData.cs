using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class TargetData
    {
        public SecurityIdentifier Sid { get; set; }

        public Guid ContainerGuid { get; set; }

        public int SortOrder { get; set; }

        public string Target { get; set; }
    }
}
