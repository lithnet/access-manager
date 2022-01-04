using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class RoleAuthorizationInformation
    {
        public IActiveDirectoryUser User { get; set; }

        public IList<RoleSecurityDescriptorTarget> MatchedTargets { get; set;  } = new List<RoleSecurityDescriptorTarget>();
    }
}
