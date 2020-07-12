using System;

namespace Lithnet.AccessManager.Server.Configuration
{
    [Flags]
    public enum AuditReasonFieldState
    {
        Hidden = 0,
        Optional = 1,
        Required = 2
    }
}