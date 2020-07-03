using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Configuration
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
