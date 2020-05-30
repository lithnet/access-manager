using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using IdentityModel.Client;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.WsFederation;
using NLog;
using Owin;
using Unity;

//[assembly: OwinStartup(typeof(Startup), "Configure")]

namespace Lithnet.Laps.Web
{
    public class Startup
    {
        internal static bool CanLogout = false;

        private static IExternalAuthenticationProvider authProvider;

        private readonly ILogger logger;

        private readonly IReporting reporting;

        private readonly IDirectory directory;

        /// <summary>
        /// Explicitly get the logger and the reporting from the DI-container.
        /// Constructor injection doesn't work for this class.
        /// </summary>
        public Startup()
        {
            IUnityContainer container = UnityConfig.Container;
            LapsConfigSection section = (LapsConfigSection)ConfigurationManager.GetSection(LapsConfigSection.SectionName);
            container.RegisterInstance<ILapsConfig>(section);

            this.logger = container.Resolve<ILogger>();
            this.reporting = container.Resolve<IReporting>();
            this.directory = container.Resolve<IDirectory>();
        }

        public void Configure(IAppBuilder app)
        {
            var authSettings = UnityConfig.Container.Resolve<IAuthenticationSettings>();

            //var x = UnityConfig.Container.Resolve<IRateLimits>();

            IdentityModelEventSource.ShowPII = authSettings.ShowPii;

            if (authSettings.Mode == "wsfed")
            {
                this.ConfigureWsFederation(app);
            }
            else if (authSettings.Mode == "oidc")
            {
                this.ConfigureOpenIDConnect(app);
            }
            else
            {
                this.ConfigureWindowsAuth(app);
            }
        }

        public void ConfigureOpenIDConnect(IAppBuilder app)
        {
            Startup.CanLogout = true;
            var oidcSettings = UnityConfig.Container.Resolve<IOidcSettings>();

            Startup.authProvider = oidcSettings;
            AntiForgeryConfig.UniqueClaimTypeIdentifier = oidcSettings.UniqueClaimTypeIdentifier;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager(),
                AuthenticationType = "Cookies"
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = oidcSettings.ClientID,
                ClientSecret = oidcSettings.Secret,
                Authority = oidcSettings.Authority,
                RedirectUri = oidcSettings.RedirectUri,
                ResponseType = oidcSettings.ResponseType,
                Scope = OpenIdConnectScope.OpenIdProfile,
                PostLogoutRedirectUri = oidcSettings.PostLogourRedirectUri,
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    SaveSigninToken = true
                },

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = async n =>
                    {
                        try
                        {
                            OpenIdConnectConfiguration config = await n.Options.ConfigurationManager.GetConfigurationAsync(n.Request.CallCancelled).ConfigureAwait(false);

                            HttpClient client = new HttpClient();

                            TokenResponse tokenResponse = await client.RequestTokenAsync(new TokenRequest
                            {
                                Address = config.TokenEndpoint,
                                ClientId = oidcSettings.ClientID,
                                ClientSecret = oidcSettings.Secret,
                            });

                            if (tokenResponse.IsError)
                            {
                                throw new Exception(tokenResponse.Error);
                            }

                            UserInfoResponse userInfoResponse = await client.GetUserInfoAsync(new UserInfoRequest
                            {
                                Address = config.UserInfoEndpoint,
                                Token = tokenResponse.AccessToken
                            });

                            if (userInfoResponse.IsError)
                            {
                                throw new OpenIdConnectProtocolException(userInfoResponse.Error);
                            }

                            List<Claim> claims = new List<Claim>();
                            claims.AddRange(userInfoResponse.Claims);
                            claims.Add(new Claim("id_token", tokenResponse.IdentityToken));
                            claims.Add(new Claim("access_token", tokenResponse.AccessToken));

                            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                            {
                                claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                            }

                            n.AuthenticationTicket.Identity.AddClaims(claims);
                        }
                        catch (Exception ex)
                        {
                            this.reporting.LogErrorEvent(EventIDs.OidcAuthZCodeError, LogMessages.AuthZCodeFlowError, ex);
                            n.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(ex.Message)}");
                        }
                    },
                    SecurityTokenValidated = n =>
                    {
                        ClaimsIdentity user = n.AuthenticationTicket.Identity;
                        user.AddClaim(new Claim("id_token", n.ProtocolMessage.IdToken));
                        return this.FindClaimIdentityInDirectoryOrFail(n);
                    },
                    RedirectToIdentityProvider = n =>
                    {
                        // If signing out, add the id_token_hint
                        if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
                        {
                            Claim idTokenClaim = n.OwinContext.Authentication.User.FindFirst("id_token");

                            if (idTokenClaim != null)
                            {
                                n.ProtocolMessage.IdTokenHint = idTokenClaim.Value;
                            }
                        }

                        this.logger.Trace($"Redirecting to IdP for {n.ProtocolMessage.RequestType}");
                        return Task.CompletedTask;
                    },
                    AuthenticationFailed = this.HandleAuthNFailed
                },
            });
        }

        public void ConfigureWindowsAuth(IAppBuilder app)
        {
            Startup.CanLogout = false;
            var iwaSettings = UnityConfig.Container.Resolve<IIwaSettings>();

            AntiForgeryConfig.UniqueClaimTypeIdentifier = iwaSettings.UniqueClaimTypeIdentifier;
            Startup.authProvider = iwaSettings;
        }

        public void ConfigureWsFederation(IAppBuilder app)
        {
            Startup.CanLogout = true;
            var wsfSettings = UnityConfig.Container.Resolve<IWsFedSettings>();

            Startup.authProvider = wsfSettings;
            AntiForgeryConfig.UniqueClaimTypeIdentifier = wsfSettings.UniqueClaimTypeIdentifier;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieManager = new SystemWebCookieManager(),
                AuthenticationType = "Cookies"
            });

            app.UseWsFederationAuthentication(
                new WsFederationAuthenticationOptions
                {
                    Wtrealm = wsfSettings.Realm,
                    MetadataAddress = wsfSettings.Metadata,
                    SignOutWreply = wsfSettings.SignOutWReply,
                    Notifications = new WsFederationAuthenticationNotifications
                    {
                        SecurityTokenValidated = this.FindClaimIdentityInDirectoryOrFail,
                        AuthenticationFailed = this.HandleAuthNFailed
                    }
                });
        }

        private Task HandleAuthNFailed<TMessage, TOptions>(AuthenticationFailedNotification<TMessage, TOptions> context)
        {
            this.reporting.LogErrorEvent(EventIDs.OwinAuthNError, LogMessages.AuthNProviderError, context.Exception);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(context.Exception?.Message ?? "Unknown error")}");

            return Task.FromResult(0);
        }

        private Task FindClaimIdentityInDirectoryOrFail<TMessage, TOptions>(SecurityTokenValidatedNotification<TMessage, TOptions> context)
        {
            ClaimsIdentity user = context.AuthenticationTicket.Identity;

            string sid = this.FindUserByClaim(user, Startup.authProvider.ClaimName)?.Sid?.Value;

            if (sid == null)
            {
                string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                this.reporting.LogErrorEvent(EventIDs.SsoIdentityNotFound, message, null);

                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?message={HttpUtility.UrlEncode(UIMessages.SsoIdentityNotFound)}");
                return Task.CompletedTask;
            }

            user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));

            this.reporting.LogSuccessEvent(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));

            return Task.CompletedTask;
        }

        private IUser FindUserByClaim(ClaimsIdentity p, string claimName)
        {
            Claim c = p.FindFirst(claimName);

            if (c != null)
            {
                return this.directory.GetUser(c.Value);
            }

            return null;
        }
    }
}