using System;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public class OidcAuthenticationProvider : IdpAuthenticationProvider, IOidcAuthenticationProvider
    {
        private readonly OidcAuthenticationProviderOptions options;

        public OidcAuthenticationProvider(IOptions<OidcAuthenticationProviderOptions> options, ILogger<OidcAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider)
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
             .AddOpenIdConnect("laps", openIdConnectOptions =>
             {
                 openIdConnectOptions.Authority = this.options.Authority;
                 openIdConnectOptions.ClientId = this.options.ClientID;
                 openIdConnectOptions.ClientSecret = this.options.Secret?.GetSecret();
                 openIdConnectOptions.CallbackPath = "/auth";
                 openIdConnectOptions.SignedOutCallbackPath = "/auth/logout";
                 openIdConnectOptions.SignedOutRedirectUri = "/Home/LoggedOut";
                 openIdConnectOptions.ResponseType = this.options.ResponseType ?? OpenIdConnectResponseType.Code;
                 openIdConnectOptions.SaveTokens = true;
                 openIdConnectOptions.GetClaimsFromUserInfoEndpoint = this.options.GetUserInfoEndpointClaims ?? openIdConnectOptions.ResponseType.Contains(OpenIdConnectResponseType.Code);
                 openIdConnectOptions.UseTokenLifetime = true;
                 openIdConnectOptions.Events = new OpenIdConnectEvents()
                 {
                     OnRemoteFailure = this.HandleRemoteFailure,
                     OnAccessDenied = this.HandleAuthNFailed,
                     OnTicketReceived = this.FindClaimIdentityInDirectoryOrFail,
                 };

                 openIdConnectOptions.ClaimActions.MapAll();
                 openIdConnectOptions.ClaimActions.MapJsonKey(ClaimTypes.Upn, "upn");

                 openIdConnectOptions.Scope.Clear();
                 if (this.options.Scopes == null || this.options.Scopes.Count == 0)
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