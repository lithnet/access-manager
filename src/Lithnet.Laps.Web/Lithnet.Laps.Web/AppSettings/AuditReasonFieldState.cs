using System;

namespace Lithnet.Laps.Web.AppSettings
{
    [Flags]
    public enum AuditReasonFieldState
    {
        Hidden = 0,
        Optional = 1,
        Required = 2
    }
}