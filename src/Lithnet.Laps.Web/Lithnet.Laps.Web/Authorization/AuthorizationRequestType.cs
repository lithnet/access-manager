using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Authorization
{
    [Flags]
    public enum AuthorizationRequestType
    {
        [Description("Local admin password")]
        LocalAdminPassword = 1,

        [Description("Just-in-time access")]
        JitAccess = 2
    }
}
