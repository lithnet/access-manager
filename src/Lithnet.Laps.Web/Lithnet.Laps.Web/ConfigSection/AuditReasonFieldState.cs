using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.ConfigSection
{
    [Flags]
    public enum AuditReasonFieldState
    {
        NotRequired = 0,
        Requested = 1,
        Required = 2
    }
}