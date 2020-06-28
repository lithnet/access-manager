using System.Security.Claims;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Configuration
{
    public abstract class AuthenticationProviderOptions
    {
        public abstract string ClaimName { get; set; }

        public abstract bool IdpLogout { get; set; }
    }
}