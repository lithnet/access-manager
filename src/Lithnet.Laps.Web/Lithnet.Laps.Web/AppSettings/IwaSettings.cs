using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.AppSettings
{
    public class IwaSettings : IIwaSettings
    {
        private IConfigurationRoot configuration;

        public IwaSettings(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
        }

        public string UniqueClaimTypeIdentifier => this.configuration["authentication:iwa:uniqueClaimTypeIdentifier"] ?? ClaimTypes.PrimarySid;

        public string ClaimName => ClaimTypes.PrimarySid;

        public IdentityType ClaimType => IdentityType.Sid;
    }
}