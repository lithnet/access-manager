using System;

namespace Lithnet.AccessManager.Server.Configuration
{
    [Flags]
    public enum AuthenticationSchemes
    {
        None = 0,
        Basic = 1,
        NTLM = 4,
        Negotiate = 8,
        Kerberos = 16
    }
}
