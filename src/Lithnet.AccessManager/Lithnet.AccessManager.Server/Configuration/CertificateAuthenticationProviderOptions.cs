using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class CertificateAuthenticationProviderOptions : AuthenticationProviderOptions
    {
        public List<string> RequiredEkus { get; set; } = new List<string>();

        [JsonConverter(typeof(StringEnumConverter))]
        public ClientCertificateValidationMethod ValidationMethod { get; set; } = ClientCertificateValidationMethod.NtAuthStore;

        public bool RequireSmartCardLogonEku { get; set; } = true;

        public List<string> TrustedIssuers { get; set; } = new List<string>();

        [JsonConverter(typeof(StringEnumConverter))]
        public CertificateIdentityResolutionMode IdentityResolutionMode { get; set; } = CertificateIdentityResolutionMode.Default;
    }
}