using System.Security.Claims;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
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
        private readonly IProtectedSecretProvider secretProvider;

        public OidcAuthenticationProvider(IOptions<OidcAuthenticationProviderOptions> options, ILogger<OidcAuthenticationProvider> logger, IDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider, IProtectedSecretProvider secretProvider)
            : base(logger, directory, httpContextAccessor, authzContextProvider)
        {
            this.secretProvider = secretProvider;
            this.options = options.Value;
        }

        public override bool CanLogout => true;

        public override bool IdpLogout => this.options.IdpLogout;

        protected override string ClaimName => this.options.ClaimName ?? ClaimTypes.Upn;

        private Task BeforeRedirectToIdentityProviderAsync(RedirectContext context)
        {
            Claim idTokenClaim = context.HttpContext.User.FindFirst("id_token");

            if (idTokenClaim != null)
            {
                context.ProtocolMessage.IdTokenHint = idTokenClaim.Value;
            }

            return Task.CompletedTask;
        }

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
                 openIdConnectOptions.ClientSecret = this.secretProvider.UnprotectSecret(this.options.Secret);
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
                     OnRedirectToIdentityProviderForSignOut = this.BeforeRedirectToIdentityProviderAsync,
                 };

                 if (this.options.ClaimMapping == null || this.options.ClaimMapping.Count > 0)
                 {
                     openIdConnectOptions.ClaimActions.MapJsonKey(ClaimTypes.Upn, "upn");
                 }
                 else
                 {
                     foreach (var kvp in this.options.ClaimMapping)
                     {
                         openIdConnectOptions.ClaimActions.MapJsonKey(kvp.Value, kvp.Key);
                     }
                 }

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
                 cookieAuthenticationOptions.LogoutPath = "/Home/SignOut";
                 cookieAuthenticationOptions.Cookie.SameSite = SameSiteMode.None;
                 cookieAuthenticationOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                 cookieAuthenticationOptions.SessionStore = new AuthenticationTicketCache();
             });
        }
    }
}