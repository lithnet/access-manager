using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class OidcAuthenticationProvider : IdpAuthenticationProvider, IOidcAuthenticationProvider
    {
        private readonly OidcAuthenticationProviderOptions options;

        public OidcAuthenticationProvider(IOptions<OidcAuthenticationProviderOptions> options, ILogger<OidcAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            : base(logger, directory, httpContextAccessor)
        {
            this.options = options.Value;
        }
    
        public override bool CanLogout => true;

        public override bool IdpLogout => this.options.IdpLogout;

        protected override string ClaimName => this.options.ClaimName;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
             .AddOpenIdConnect("laps", openIdConnectOptions =>
             {
                 openIdConnectOptions.Authority = this.options.Authority;
                 openIdConnectOptions.ClientId = this.options.ClientID;
                 openIdConnectOptions.ClientSecret = this.options.Secret?.GetSecret();
                 openIdConnectOptions.CallbackPath = "/auth";
                 openIdConnectOptions.SignedOutCallbackPath = "/auth/logout";
                 openIdConnectOptions.SignedOutRedirectUri = "/Home/LoggedOut";
                 openIdConnectOptions.ResponseType = this.options.ResponseType;
                 openIdConnectOptions.SaveTokens = true;
                 openIdConnectOptions.GetClaimsFromUserInfoEndpoint = true;
                 openIdConnectOptions.UseTokenLifetime = true;
                 openIdConnectOptions.Events = new OpenIdConnectEvents()
                 {
                     OnTokenValidated = this.FindClaimIdentityInDirectoryOrFail,
                     OnRemoteFailure = this.HandleRemoteFailure,
                     OnAccessDenied = this.HandleAuthNFailed,
                 };

                 openIdConnectOptions.Scope.Clear();
                 if (this.options?.Scopes.Count == 0)
                 {
                     openIdConnectOptions.Scope.Add("openid");
                     openIdConnectOptions.Scope.Add("profile");
                 }
                 else
                 {
                     foreach (var scope in this.options.Scopes)
                     {
                         openIdConnectOptions.Scope.Add(scope);
                     }
                 }
             })
             .AddCookie(cookieAuthenticationOptions =>
             {
                 cookieAuthenticationOptions.LoginPath = "/Home/Login";
                 cookieAuthenticationOptions.LogoutPath = "/Home/Logout";
             });
        }
    }
}