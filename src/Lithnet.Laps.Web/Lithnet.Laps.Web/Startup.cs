using System;
using System.Configuration;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Internal;
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
using System.Threading.Channels;

namespace Lithnet.Laps.Web
{
    public class Startup
    {
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
            services.TryAddScoped<IIwaAuthenticationProvider, IwaAuthenticationProvider>();
            services.TryAddScoped<IOidcAuthenticationProvider, OidcAuthenticationProvider>();
            services.TryAddScoped<IWsFedAuthenticationProvider, WsFedAuthenticationProvider>();
            services.TryAddScoped<IUserInterfaceSettings, UserInterfaceSettings>();
            services.TryAddScoped<IRateLimitSettings, RateLimitSettings>();
            services.TryAddScoped<IAuthenticationSettings, AuthenticationSettings>();
            services.TryAddScoped<IEmailSettings, EmailSettings>();
            services.TryAddScoped<IAuditSettings, AuditSettings>();
            services.TryAddScoped<IJsonTargetsProvider, JsonFileTargetsProvider>();
            services.TryAddScoped<IAuthorizationService, BuiltInAuthorizationService>();
            services.TryAddScoped<IAuthorizationSettings, AuthorizationSettings>();
            services.TryAddScoped<JsonTargetAuthorizationService, JsonTargetAuthorizationService>();
            services.TryAddScoped<PowershellAuthorizationService, PowershellAuthorizationService>();
            services.TryAddScoped<IDirectory, ActiveDirectory.ActiveDirectory>();
            services.TryAddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.TryAddScoped<ITemplateProvider, TemplateProvider>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IAceEvaluator, AceEvaluator>();

            services.AddScoped<INotificationChannel, SmtpNotificationChannel>();
            services.AddScoped<INotificationChannel, WebhookNotificationChannel>();
            services.AddScoped<INotificationChannel, PowershellNotificationChannel>();

            services.TryAddScoped<IXffHandlerSettings, XffHandlerSettings>();

            var backgroundProcessingChannel = Channel.CreateUnbounded<Action>();

            services.AddSingleton(backgroundProcessingChannel.Reader);
            services.AddSingleton(backgroundProcessingChannel.Writer);
            services.AddHostedService<AuditWorker>();

            this.ConfigureAuthentication(services);
            this.ConfigureXff(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
            IAuthenticationProvider authProvider;

            switch (authSettings.Mode)
            {
                case "oidc":
                    authProvider = provider.GetService<IOidcAuthenticationProvider>();
                    services.TryAddSingleton<IAuthenticationProvider>(authProvider);
                    this.ConfigureOpenIDConnect(services, (IOidcAuthenticationProvider)authProvider);
                    break;

                case "wsfed":
                    authProvider = provider.GetService<IWsFedAuthenticationProvider>();
                    services.TryAddSingleton<IAuthenticationProvider>(authProvider);
                    this.ConfigureWsFederation(services, (IWsFedAuthenticationProvider)authProvider);
                    break;

                case "iwa":
                    authProvider = provider.GetService<IIwaAuthenticationProvider>();
                    services.TryAddSingleton<IAuthenticationProvider>(authProvider);
                    this.ConfigureWindowsAuth(services, (IIwaAuthenticationProvider)authProvider);
                    break;

                default:
                    throw new ConfigurationErrorsException("The authentication mode setting in the configuration file was unknown");
            }
        }

        private void ConfigureOpenIDConnect(IServiceCollection services, IOidcAuthenticationProvider provider)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddOpenIdConnect("laps", options =>
            {
                options.Authority = provider.Authority;
                options.ClientId = provider.ClientID;
                options.ClientSecret = provider.Secret;
                options.CallbackPath = "/auth";
                options.SignedOutCallbackPath = "/auth/logout";
                options.SignedOutRedirectUri = "/Home/LoggedOut";
                options.ResponseType = provider.ResponseType;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UseTokenLifetime = true;

                options.Events = new OpenIdConnectEvents()
                {
                    OnTokenValidated = provider.FindClaimIdentityInDirectoryOrFail,
                    OnRemoteFailure = provider.HandleRemoteFailure,
                    OnAccessDenied = provider.HandleAuthNFailed,
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Home/Login";
                options.LogoutPath = "/Home/Logout";
            });
        }

        private void ConfigureWindowsAuth(IServiceCollection services, IIwaAuthenticationProvider provider)
        {
            services.AddAuthentication(HttpSysDefaults.AuthenticationScheme);
        }

        private void ConfigureWsFederation(IServiceCollection services, IWsFedAuthenticationProvider provider)
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
                options.MetadataAddress = provider.Metadata;
                options.Wtrealm = provider.Realm;
                options.Events = new WsFederationEvents()
                {
                    OnSecurityTokenValidated = provider.FindClaimIdentityInDirectoryOrFail,
                    OnAccessDenied = provider.HandleAuthNFailed,
                    OnRemoteFailure = provider.HandleRemoteFailure
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
