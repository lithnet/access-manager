using System;
using System.Configuration;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.App_LocalResources;
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
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.HttpOverrides;
using System.Linq;

namespace Lithnet.Laps.Web
{
    public class Startup
    {
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
            services.TryAddScoped<IEmailSettings, EmailSettings>();
            services.TryAddScoped<GlobalAuditSettings, GlobalAuditSettings>();
            services.TryAddScoped<IJsonTargetsProvider, JsonFileTargetsProvider>();
            services.TryAddScoped<IAuthorizationService, BuiltInAuthorizationService>();
            services.TryAddScoped<IAuthorizationSettings, AuthorizationSettings>();
            services.TryAddScoped<JsonTargetAuthorizationService, JsonTargetAuthorizationService>();
            services.TryAddScoped<PowershellAuthorizationService, PowershellAuthorizationService>();
            services.TryAddScoped<IDirectory, ActiveDirectory.ActiveDirectory>();
            services.TryAddScoped<IReporting, Reporting>();
            services.TryAddScoped<ITemplates, TemplatesFromFiles>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IMailer, SmtpMailer>();
            services.TryAddScoped<IXffHandlerSettings, XffHandlerSettings>();
            this.ConfigureAuthentication(services);
            this.ConfigureXff(services);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IReporting reporting, IDirectory directory, ILogger logger)
        {
            this.reporting = reporting;
            this.directory = directory;
            this.logger = logger;

            app.UseForwardedHeaders();

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

        private void ConfigureXff(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var settings = provider.GetService<IXffHandlerSettings>();
            
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardedForHeaderName = settings.ForwardedForHeaderName;
                options.ForwardedProtoHeaderName = settings.ForwardedProtoHeaderName;
                options.ForwardedHostHeaderName = settings.ForwardedHostHeaderName;
                options.ForwardLimit = settings.ForwardLimit < 0 ? (int?)null : settings.ForwardLimit;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
                
                settings.TrustedProxies.ForEach(t => options.KnownProxies.Add(t));
                settings.TrustedNetworks.ForEach(t => options.KnownNetworks.Add(t));
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
                    services.TryAddSingleton(authProvider);
                    this.ConfigureOpenIDConnect(services, (IOidcSettings)authProvider);
                    break;

                case "wsfed":
                    authProvider = provider.GetService<IWsFedSettings>();
                    services.TryAddSingleton(authProvider);
                    this.ConfigureWsFederation(services, (IWsFedSettings)authProvider);
                    break;

                case "iwa":
                    authProvider = provider.GetService<IIwaSettings>();
                    services.TryAddSingleton(authProvider);
                    this.ConfigureWindowsAuth(services, (IIwaSettings)authProvider);
                    break;

                default:
                    throw new ConfigurationErrorsException("The authentication mode setting in the configuration file was unknown");
            }
        }

        private void ConfigureOpenIDConnect(IServiceCollection services, IOidcSettings oidcSettings)
        {
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
                options.CallbackPath = "/auth";
                options.SignedOutCallbackPath = "/auth/logout";
                options.SignedOutRedirectUri = "/Home/LoggedOut";
                options.ResponseType = oidcSettings.ResponseType;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UseTokenLifetime = true;

                options.Events = new OpenIdConnectEvents()
                {
                    OnTokenValidated = FindClaimIdentityInDirectoryOrFail,
                    OnRemoteFailure = HandleRemoteFailure,
                    OnAccessDenied = HandleAuthNFailed,
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Home/Login";
                options.LogoutPath = "/Home/Logout";
            });
        }

        private void ConfigureWindowsAuth(IServiceCollection services, IIwaSettings iwaSettings)
        {
            services.AddAuthentication(HttpSysDefaults.AuthenticationScheme);
        }

        private void ConfigureWsFederation(IServiceCollection services, IWsFedSettings wsfSettings)
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
                options.MetadataAddress = wsfSettings.Metadata;
                options.Wtrealm = wsfSettings.Realm;
                options.Events = new WsFederationEvents()
                {
                    OnSecurityTokenValidated = FindClaimIdentityInDirectoryOrFail,
                    OnAccessDenied = HandleAuthNFailed,
                    OnRemoteFailure = HandleRemoteFailure
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
            this.reporting.LogErrorEvent(EventIDs.ExternalAuthNAccessDenied, LogMessages.AuthNAccessDenied, context.Result?.Failure);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderDenied}");

            return Task.CompletedTask;
        }


        private Task HandleRemoteFailure(RemoteFailureContext context)
        {
            this.reporting.LogErrorEvent(EventIDs.ExternalAuthNProviderError, LogMessages.AuthNProviderError, context.Failure);
            context.HandleResponse();
            context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.ExternalAuthNProviderError}");

            return Task.CompletedTask;
        }

        private Task FindClaimIdentityInDirectoryOrFail<T>(RemoteAuthenticationContext<T> context) where T : AuthenticationSchemeOptions
        {
            try
            {
                ClaimsIdentity user = context.Principal.Identity as ClaimsIdentity;
                string sid = this.FindUserByClaim(user, authProvider.ClaimName)?.Sid?.Value;

                if (sid == null)
                {
                    string message = string.Format(LogMessages.UserNotFoundInDirectory, user.ToClaimList());
                    this.reporting.LogErrorEvent(EventIDs.SsoIdentityNotFound, message, null);
                    context.HandleResponse();
                    context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                    return Task.CompletedTask;
                }

                user.AddClaim(new Claim(ClaimTypes.PrimarySid, sid));
                this.reporting.LogSuccessEvent(EventIDs.UserAuthenticated, string.Format(LogMessages.AuthenticatedAndMappedUser, user.ToClaimList()));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                this.reporting.LogErrorEvent(EventIDs.AuthNResponseProcessingError, LogMessages.AuthNResponseProcessingError, ex);
                context.HandleResponse();
                context.Response.Redirect($"/Home/AuthNError?messageid={(int)AuthNFailureMessageID.SsoIdentityNotFound}");
                return Task.CompletedTask;
            }
        }

        private IUser FindUserByClaim(ClaimsIdentity p, string claimName)
        {
            Claim c = p.FindFirst(claimName);

            if (c != null)
            {
                this.logger.Trace($"Attempting to find a match in the directory for externally provided claim {c.Type}:{c.Value}");

                try
                {
                    return this.directory.GetUser(c.Value);
                }
                catch (Exception ex)
                {
                    this.reporting.LogErrorEvent(EventIDs.AuthNDirectoryLookupError, string.Format(LogMessages.AuthNDirectoryLookupError, c.Type, c.Value), ex);
                }
            }

            return null;
        }
    }
}
