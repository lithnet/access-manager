using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public class WsFedAuthenticationProvider : IdpAuthenticationProvider, IWsFedAuthenticationProvider
    {
        private readonly WsFedAuthenticationProviderOptions options;

        public WsFedAuthenticationProvider(IOptions<WsFedAuthenticationProviderOptions> options, ILogger<WsFedAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider)
            :base (logger, directory, httpContextAccessor, authzContextProvider)
        {
            this.options = options.Value;
        }
        
        public override bool CanLogout => true;

        public override bool IdpLogout => this.options.IdpLogout;

        protected override string ClaimName => this.options.ClaimName;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddWsFederation("laps", options =>
            {
                options.CallbackPath = "/auth";
                options.MetadataAddress = this.options.Metadata;
                options.Wtrealm = this.options.Realm;
                options.Events = new WsFederationEvents()
                {
                    OnSecurityTokenValidated = this.FindClaimIdentityInDirectoryOrFail,
                    OnAccessDenied = this.HandleAuthNFailed,
                    OnRemoteFailure = this.HandleRemoteFailure
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Home/Login";
                options.LogoutPath = "/Home/SignOut";
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
        }
    }
}