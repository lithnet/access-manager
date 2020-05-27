using System;

namespace Lithnet.Laps.Web.Config
{
    [Flags]
    public enum AuditReasonFieldState
    {
        Hidden = 0,
        Optional = 1,
        Required = 2
    }
}