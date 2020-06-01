using System;
using System.Configuration;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Core.App_LocalResources;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NLog;
using AuthenticationService = Lithnet.Laps.Web.Internal.AuthenticationService;
using IAuthenticationService = Lithnet.Laps.Web.Internal.IAuthenticationService;

namespace Lithnet.Laps.Web.Core
{
    public class Startup
    {
        public static bool CanLogout = false;

        private static IExternalAuthProviderSettings authProvider;

        private ILogger logger;

        private IReporting reporting;

        private IDirectory directory;

        public Startup()
        {
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();

            services.TryAddTransient<ILogger>(_ => LogManager.GetCurrentClassLogger());
            services.TryAddScoped<IIwaSettings, IwaSettings>();
            services.TryAddScoped<IOidcSettings, OidcSettings>();
            services.TryAddScoped<IWsFedSettings, WsFedSettings>();
            services.TryAddScoped<IUserInterfaceSettings, UserInterfaceSettings>();
            services.TryAddScoped<IRateLimitSettings, RateLimitSettings>();
            services.TryAddScoped<IAuthenticationSettings, AuthenticationSettings>();
            services.TryAddScoped<IIpResolverSettings, IpResolverSettings>();
            services.TryAddScoped<IEmailSettings, EmailSettings>();
            services.TryAddScoped<IIpAddressResolver, IpAddressResolver>();
            services.TryAddScoped<GlobalAuditSettings, GlobalAuditSettings>();
            services.TryAddScoped<IJsonTargetsProvider, JsonFileTargetsProvider>();
            services.TryAddScoped<IAuthorizationService, BuiltInAuthorizationService>();
            services.TryAddScoped<IAuthorizationSettings, AuthorizationSettings>();
            services.TryAddScoped<JsonTargetAuthorizationService, JsonTargetAuthorizationService>();
            services.TryAddScoped<PowershellAuthorizationService, PowershellAuthorizationService>();
            services.TryAddScoped<IDirectory, ActiveDirectory.ActiveDirectory>();
            services.TryAddScoped<IAuthenticationService, AuthenticationService>();
            services.TryAddScoped<IReporting, Reporting>();
            services.TryAddScoped<ITemplates, TemplatesFromFiles>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IMailer, SmtpMailer>();
            this.ConfigureAuthentication(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IReporting reporting, IDirectory directory, ILogger logger)
        {
            this.reporting = reporting;
            this.directory = directory;
            this.logger = logger;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        private void ConfigureAuthentication(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            var authSettings = provider.GetService<IAuthenticationSettings>();

            switch (authSettings.Mode)
            {
                case "oidc":
                    authProvider = provider.GetService<IOidcSettings>();
                    this.ConfigureOpenIDConnect(services, (IOidcSettings)authProvider);
                    break;

                case "wsfed":
                    authProvider = provider.GetService<IWsFedSettings>();
                    this.ConfigureWsFederation(services, (IWsFedSettings)authProvider);
                    break;

                case "iwa":
                    authProvider = provider.GetService<IIwaSettings>();
                    this.ConfigureWindowsAuth(services, (IIwaSettings)authProvider);
                    break;

                default:
                    throw new ConfigurationErrorsException("The authentication mode setting in the configuration file was unknown");
            }
        }

        private void ConfigureOpenIDConnect(IServiceCollection services, IOidcSettings oidcSettings)
        {
            CanLogout = true;

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })

            .AddOpenIdConnect("laps", options =>
            {
                options.Authority = oidcSettings.Authority;
                options.ClientId = oidcSettings.ClientID;
                options.ClientSecret = oidcSettings.Secret;
                options.SignedOutRedirectUri = oidcSettings.PostLogourRedirectUri;
                options.CallbackPath = "/oidc";
                options.ResponseType = oidcSettings.ResponseType;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.AccessDeniedPath = new PathString("/Home/AuthNError");
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UseTokenLifetime = true;

                options.Events = new OpenIdConnectEvents()
                {
                    OnTokenValidated = FindClaimIdentityInDirectoryOrFail,
                    OnAccessDenied = HandleAuthNFailed
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Home/Login";
                options.LogoutPath = "/Home/SignOut";
                //options.Cookie.SameSite = SameSiteMode.None;
                // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            //services.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            //{


            //    Notifications = new OpenIdConnectAuthenticationNotifications
            //    {
            //        RedirectToIdentityProvider = n =>
            //        {
            //            // If signing out, add the id_token_hint
            //            if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            //            {
            //                Claim idTokenClaim = n.OwinContext.Authentication.User.FindFirst("id_token");

            //                if (idTokenClaim != null)
            //                {
            //                    n.ProtocolMessage.IdTokenHint = idTokenClaim.Value;
            //                }
            //            }

            //            this.logger.Trace($"Redirecting to IdP for {n.ProtocolMessage.RequestType}");
            //            return Task.CompletedTask;
            //        },
            //        AuthenticationFailed = this.HandleAuthNFailed
            //    },
            //});
        }

        private void ConfigureWindowsAuth(IServiceCollection services, IIwaSettings iwaSettings)
        {
            Startup.CanLogout = false;
        }

        private void ConfigureWsFederation(IServiceCollection services, IWsFedSettings wsfSettings)
        {
            CanLogout = true;
         
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
            })

            .AddWsFederation("laps", options =>
            {
                options.CallbackPath = "/callback";
                options.MetadataAddress = wsfSettings.Metadata;
                options.SignOutWreply = wsfSettings.SignOutWReply;
                options.Wtrealm = wsfSettings.Realm;
                options.AccessDeniedPath = "/Home/AuthNError";
                options.Events = new WsFederationEvents()
                {
                    OnSecurityTokenValidated = FindClaimIdentityInDirectoryOrFail,
                    OnAccessDenied = HandleAuthNFailed
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

        private Task HandleAuthNFailed(AccessDeniedContext context)
        {
            this.reporting.LogErrorEvent(EventIDs.OwinAuthNError, LogMessages.AuthNProviderError, context.Result.Failure);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?message={WebUtility.UrlEncode(context.Result.Failure?.Message ?? "Unknown error")}");

            return Task.CompletedTask;
        }

        private Task FindClaimIdentityInDirectoryOrFail<T>(RemoteAuthenticationContext<T> context) where T : AuthenticationSchemeOptions
        {
            ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;

            string sid = this.FindUserByClaim(user, Startup.authProvider.ClaimName)?.Sid?.Value;

            if (sid == null)
            {
                string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                this.reporting.LogErrorEvent(EventIDs.SsoIdentityNotFound, message, null);

                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?message={WebUtility.UrlEncode(UIMessages.SsoIdentityNotFound)}");
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
