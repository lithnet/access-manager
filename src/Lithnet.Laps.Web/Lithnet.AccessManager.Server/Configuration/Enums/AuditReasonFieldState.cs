using System;

namespace Lithnet.AccessManager.Configuration
{
    [Flags]
    public enum AuditReasonFieldState
    {
        Hidden = 0,
        Optional = 1,
        Required = 2
    }
}