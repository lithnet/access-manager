using System.Collections.Generic;
using System.Security.Claims;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class CertificateAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public override string ClaimName { get; set; } = ClaimTypes.PrimarySid;

        public override bool IdpLogout { get; set; } = false;

        public string RequiredCustomEku { get; set; }

        public bool MustValidateToNTAuth { get; set; }

        public bool RequireSmartCardLogonEku { get; set; }

        public List<string> IssuerThumbprints { get; set; }
    }
}