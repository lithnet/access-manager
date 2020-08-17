using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Channels;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Workers;
using Lithnet.AccessManager.Service.AppSettings;
using Lithnet.AccessManager.Service.Internal;
using Lithnet.AccessManager.Service.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddResponseCaching();
            services.TryAddScoped<IIwaAuthenticationProvider, IwaAuthenticationProvider>();
            services.TryAddScoped<IOidcAuthenticationProvider, OidcAuthenticationProvider>();
            services.TryAddScoped<IWsFedAuthenticationProvider, WsFedAuthenticationProvider>();
            services.TryAddScoped<ICertificateAuthenticationProvider, CertificateAuthenticationProvider>();

            services.TryAddScoped<IAuthorizationService, SecurityDescriptorAuthorizationService>();
            services.TryAddScoped<SecurityDescriptorAuthorizationService>();
            services.TryAddScoped<IPowerShellSecurityDescriptorGenerator, PowerShellSecurityDescriptorGenerator>();
            services.TryAddSingleton<IDirectory, ActiveDirectory>();
            services.TryAddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.TryAddScoped<ITemplateProvider, TemplateProvider>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IJitAccessProvider, JitAccessProvider>();
            services.TryAddScoped<IPhoneticPasswordTextProvider, PhoneticStringProvider>();
            services.TryAddScoped<IHtmlPasswordProvider, HtmlPasswordProvider>();
            services.TryAddScoped<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>();
            services.TryAddScoped<IPasswordProvider, PasswordProvider>();
            services.TryAddScoped<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
            services.TryAddScoped<IEncryptionProvider, EncryptionProvider>();
            services.TryAddScoped<ICertificateProvider, CertificateProvider>();
            services.TryAddScoped<IAuthorizationInformationBuilder, AuthorizationInformationBuilder>();
            services.TryAddScoped<ITargetDataProvider, TargetDataProvider>();

            services.TryAddSingleton<IAppPathProvider, WebAppPathProvider>();
            services.TryAddSingleton<RandomNumberGenerator>(RandomNumberGenerator.Create());
            services.TryAddSingleton<IJitAccessGroupResolver, JitAccessGroupResolver>();
            services.TryAddSingleton<IPowerShellSessionProvider, CachedPowerShellSessionProvider>();
            services.TryAddSingleton<IAuthorizationInformationMemoryCache, AuthorizationInformationMemoryCache>();
            services.TryAddSingleton<ITargetDataCache, TargetDataCache>();
            services.TryAddSingleton<IAuthorizationContextProvider, AuthorizationContextProvider>();

            services.AddScoped<INotificationChannel, SmtpNotificationChannel>();
            services.AddScoped<INotificationChannel, WebhookNotificationChannel>();
            services.AddScoped<INotificationChannel, PowershellNotificationChannel>();

            var backgroundProcessingChannel = Channel.CreateUnbounded<Action>();

            services.AddSingleton(backgroundProcessingChannel.Reader);
            services.AddSingleton(backgroundProcessingChannel.Writer);
            services.AddHostedService<AuditWorker>();
            services.AddHostedService<JitGroupWorker>();

            services.Configure<UserInterfaceOptions>(Configuration.GetSection("UserInterface"));
            services.Configure<RateLimitOptions>(Configuration.GetSection("RateLimits"));
            services.Configure<EmailOptions>(Configuration.GetSection("Email"));
            services.Configure<AuditOptions>(Configuration.GetSection("Auditing"));
            services.Configure<AuthenticationOptions>(Configuration.GetSection("Authentication"));
            services.Configure<IwaAuthenticationProviderOptions>(Configuration.GetSection("Authentication:Iwa"));
            services.Configure<OidcAuthenticationProviderOptions>(Configuration.GetSection("Authentication:Oidc"));
            services.Configure<WsFedAuthenticationProviderOptions>(Configuration.GetSection("Authentication:WsFed"));
            services.Configure<CertificateAuthenticationProviderOptions>(Configuration.GetSection("Authentication:ClientCert"));
            services.Configure<HostingOptions>(Configuration.GetSection("Hosting"));
            services.Configure<AuthorizationOptions>(Configuration.GetSection("Authorization"));
            services.Configure<ForwardedHeadersAppOptions>(Configuration.GetSection("ForwardedHeaders"));
            services.Configure<JitConfigurationOptions>(Configuration.GetSection("JitConfiguration"));

            this.ConfigureAuthentication(services);
            this.ConfigureAuthorization(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
        {
            var fwdOptions = provider.GetRequiredService<IOptions<ForwardedHeadersAppOptions>>().Value;
            app.UseForwardedHeaders(fwdOptions.ToNativeOptions());
            app.UseStatusCodePagesWithReExecute("/StatusCode", "?code={0}");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseResponseCaching();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseFeaturePolicy();
            app.UseContentSecurityPolicy();
            app.UseReferrerPolicy();
            app.UseContentTypeOptions();
            app.UseStaticFiles();

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
            var authSettings = provider.GetService<IOptions<AuthenticationOptions>>();

            IAuthenticationProvider authProvider = authSettings.Value.Mode switch
            {
                AuthenticationMode.Iwa => provider.GetService<IIwaAuthenticationProvider>(),
                AuthenticationMode.Oidc => provider.GetService<IOidcAuthenticationProvider>(),
                AuthenticationMode.WsFed => provider.GetService<IWsFedAuthenticationProvider>(),
                AuthenticationMode.Certificate => provider.GetService<ICertificateAuthenticationProvider>(),
                _ => throw new ConfigurationErrorsException("The authentication mode setting in the configuration file was unknown")
            };

            authProvider.Configure(services);
            services.TryAddSingleton<IAuthenticationProvider>(authProvider);
        }

        private void ConfigureAuthorization(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var authSettings = provider.GetService<IOptions<AuthenticationOptions>>().Value;

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAuthorizedUser", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ClaimTypes.PrimarySid);

                    if (authSettings.AllowedPrincipals?.Count > 0)
                    {
                        policy.RequireAssertion(context =>
                        {
                            return (context.User.Claims.Any(
                                       c => string.Equals(c.Type, ClaimTypes.PrimarySid, StringComparison.OrdinalIgnoreCase) && authSettings.AllowedPrincipals.Contains(c.Value, StringComparer.Ordinal))) ||
                                   (context.User.Claims.Any(
                                       c => string.Equals(c.Type, ClaimTypes.GroupSid, StringComparison.OrdinalIgnoreCase) && authSettings.AllowedPrincipals.Contains(c.Value, StringComparer.Ordinal)));
                        });
                    }
                });
            });
        }
    }
}
