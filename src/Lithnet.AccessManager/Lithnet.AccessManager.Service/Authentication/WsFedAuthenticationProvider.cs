using System.Security.Claims;
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
            : base(logger, directory, httpContextAccessor, authzContextProvider)
        {
            this.options = options.Value;
        }

        public override bool CanLogout => true;

        public override bool IdpLogout => this.options.IdpLogout;

        protected override string ClaimName => this.options.ClaimName ?? ClaimTypes.Upn;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddWsFederation("laps", wsFederationOptions =>
            {
                wsFederationOptions.CallbackPath = "/auth";
                wsFederationOptions.MetadataAddress = this.options.Metadata;
                wsFederationOptions.Wtrealm = this.options.Realm;
                wsFederationOptions.SignOutWreply = "/Home/LoggedOut";
                wsFederationOptions.Events = new WsFederationEvents()
                {
                    OnAccessDenied = this.HandleAuthNFailed,
                    OnRemoteFailure = this.HandleRemoteFailure,
                    OnTicketReceived = this.FindClaimIdentityInDirectoryOrFail
                };
            })
            .AddCookie(cookieAuthenticationOptions =>
            {
                cookieAuthenticationOptions.LoginPath = "/Home/Login";
                cookieAuthenticationOptions.LogoutPath = "/Home/SignOut";
                cookieAuthenticationOptions.AccessDeniedPath = "/Home/AccessDenied";
                cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.None;
                cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookieAuthenticationOptions.SessionStore = new AuthenticationTicketCache();
            });
        }
    }
}