using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [Flags]
    public enum CertificateIdentityResolutionMode
    {
        Default = 0,
        UpnSan = 1,
        AltSecurityIdentities = 2,
    }
}
