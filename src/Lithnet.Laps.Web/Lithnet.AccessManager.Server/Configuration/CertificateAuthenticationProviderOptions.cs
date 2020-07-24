using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using Microsoft.AspNetCore.Authentication.Certificate;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class CertificateAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public override string ClaimName { get; set; } = ClaimTypes.PrimarySid;

        public override bool IdpLogout { get; set; } = false;

        [JsonConverter(typeof(StringEnumConverter))]
        public CertificateTypes AllowedCertificateTypes { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public X509RevocationFlag RevocationFlag { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public X509RevocationMode RevocationMode { get; set; }

        public string RequireCustomEku { get; set; }

        public bool MustValidateToNTAuth { get; set; }

        public bool RequireSmartCardLogonEku { get; set; }

        public List<string> IssuerThumbprints { get; set; }
    }
}