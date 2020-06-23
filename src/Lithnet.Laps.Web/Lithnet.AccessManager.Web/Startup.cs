using System;
using System.Configuration;
using System.Threading.Channels;
using Lithnet.AccessManager.Web.AppSettings;
using Lithnet.AccessManager.Web.Authorization;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Lithnet.AccessManager.Web
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
            services.TryAddScoped<IDirectory, ActiveDirectory>();
            services.TryAddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.TryAddScoped<ITemplateProvider, TemplateProvider>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IAceEvaluator, AceEvaluator>();
            services.TryAddScoped<IJitAccessGroupResolver, JitAccessGroupResolver>();
            services.TryAddScoped<IJitProvider, AdGroupJitProvider>();
            services.TryAddScoped<IPhoneticPasswordTextProvider, NatoPhoneticStringProvider>();
            services.TryAddScoped<IHtmlPasswordProvider, HtmlPasswordProvider>();
            services.TryAddScoped<IAppDataProvider, MsDsAppConfigurationProvider>();
            services.TryAddScoped<IPasswordProvider, PasswordProvider>();
            services.TryAddScoped<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
            services.TryAddScoped<IEncryptionProvider, EncryptionProvider>();
            services.TryAddScoped<ICertificateResolver, CertificateResolver>();

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

            //Feature-Policy
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Feature-Policy", "geolocation 'none';midi 'none';notifications 'none';push 'none';sync-xhr 'none';microphone 'none';camera 'none';magnetometer 'none';gyroscope 'none';speaker 'self';vibrate 'none';fullscreen 'self';payment 'none';");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'none'; script-src 'self'; connect-src 'self'; img-src 'self'; style-src 'self'; font-src 'self';");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                await next.Invoke();
            });

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
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
