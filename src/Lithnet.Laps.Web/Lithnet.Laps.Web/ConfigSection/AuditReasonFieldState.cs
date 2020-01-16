using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.ConfigSection
{
    [Flags]
    public enum AuditReasonFieldState
    {
        Hidden = 0,
        Optional = 1,
        Required = 2
    }
}