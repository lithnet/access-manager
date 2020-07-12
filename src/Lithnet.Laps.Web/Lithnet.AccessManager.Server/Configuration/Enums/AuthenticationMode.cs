using System.ComponentModel;

namespace Lithnet.AccessManager.Server.Configuration
{
    public enum AuthenticationMode
    {
        [Description("Integrated windows authentication")]
        Iwa = 0,

        [Description("OpenID Connect")]
        Oidc = 1,

        [Description("WS-Federation")]
        WsFed = 2
    }
}