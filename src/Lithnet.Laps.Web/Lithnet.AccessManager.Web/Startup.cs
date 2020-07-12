using System;
using System.Configuration;
using System.Threading.Channels;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Web.AppSettings;
using Lithnet.AccessManager.Web.Authorization;
using Lithnet.AccessManager.Web.Extensions;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NLog;

namespace Lithnet.AccessManager.Web
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

            services.TryAddTransient<ILogger>(_ => LogManager.GetCurrentClassLogger());
            services.TryAddScoped<IIwaAuthenticationProvider, IwaAuthenticationProvider>();
            services.TryAddScoped<IOidcAuthenticationProvider, OidcAuthenticationProvider>();
            services.TryAddScoped<IWsFedAuthenticationProvider, WsFedAuthenticationProvider>();

            services.TryAddScoped<IJsonTargetsProvider, JsonFileTargetsProvider>();
            services.TryAddScoped<IAuthorizationService, SecurityDescriptorAuthorizationService>();
            services.TryAddScoped<SecurityDescriptorAuthorizationService>();
            services.TryAddScoped<JsonTargetAuthorizationService, JsonTargetAuthorizationService>();
            services.TryAddScoped<PowershellAuthorizationService, PowershellAuthorizationService>();
            services.TryAddScoped<IDirectory, ActiveDirectory>();
            services.TryAddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.TryAddScoped<ITemplateProvider, TemplateProvider>();
            services.TryAddScoped<IRateLimiter, RateLimiter>();
            services.TryAddScoped<IAceEvaluator, AceEvaluator>();
            services.TryAddScoped<IJitAccessGroupResolver, JitAccessGroupResolver>();
            services.TryAddScoped<IJitProvider, AdGroupJitProvider>();
            services.TryAddScoped<IPhoneticPasswordTextProvider, PhoneticStringProvider>();
            services.TryAddScoped<IHtmlPasswordProvider, HtmlPasswordProvider>();
            services.TryAddScoped<IAppDataProvider, MsDsAppConfigurationProvider>();
            services.TryAddScoped<IPasswordProvider, PasswordProvider>();
            services.TryAddScoped<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
            services.TryAddScoped<IEncryptionProvider, EncryptionProvider>();
            services.TryAddScoped<ICertificateProvider, CertificateProvider>();
            services.TryAddScoped<IAppPathProvider, WebAppPathProvider>();

            services.AddScoped<INotificationChannel, SmtpNotificationChannel>();
            services.AddScoped<INotificationChannel, WebhookNotificationChannel>();
            services.AddScoped<INotificationChannel, PowershellNotificationChannel>();

            var backgroundProcessingChannel = Channel.CreateUnbounded<Action>();

            services.AddSingleton(backgroundProcessingChannel.Reader);
            services.AddSingleton(backgroundProcessingChannel.Writer);
            services.AddHostedService<AuditWorker>();

            services.Configure<UserInterfaceOptions>(Configuration.GetSection("UserInterface"));
            services.Configure<RateLimitOptions>(Configuration.GetSection("RateLimits"));
            services.Configure<EmailOptions>(Configuration.GetSection("Email"));
            services.Configure<AuditOptions>(Configuration.GetSection("Auditing"));
            services.Configure<AuthenticationOptions>(Configuration.GetSection("Authentication"));
            services.Configure<HostingOptions>(Configuration.GetSection("Hosting"));
            services.Configure<AuthorizationOptions>(Configuration.GetSection("Authorization"));
            services.Configure<ForwardedHeadersAppOptions>(Configuration.GetSection("ForwardedHeaders"));
            services.Configure<JitConfigurationOptions>(Configuration.GetSection("JitConfiguration"));

            services.Configure<JsonFileTargetsProviderOptions>(Configuration.GetSection("Authorization:JsonProvider"));
            services.Configure<PowershellAuthorizationProviderOptions>(Configuration.GetSection("Authorization:PowershellProvider"));
            services.Configure<BuiltInProviderOptions>(Configuration.GetSection("Authorization:BuiltInProvider"));

            this.ConfigureAuthentication(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
        {
            var fwdOptions = provider.GetRequiredService<IOptions<ForwardedHeadersAppOptions>>().Value;
            app.UseForwardedHeaders(fwdOptions.ToNativeOptions());

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
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseFeaturePolicy();
            app.UseContentSecurityPolicy();
            app.UseReferrerPolicy();
            app.UseContentTypeOptions();

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
            IAuthenticationProvider authProvider;

            switch (authSettings.Value.Mode)
            {
                case AuthenticationMode.Iwa:
                    authProvider = provider.GetService<IIwaAuthenticationProvider>();
                    break;

                case AuthenticationMode.Oidc:
                    authProvider = provider.GetService<IOidcAuthenticationProvider>();
                    break;

                case AuthenticationMode.WsFed:
                    authProvider = provider.GetService<IWsFedAuthenticationProvider>();
                    break;

                default:
                    throw new ConfigurationErrorsException("The authentication mode setting in the configuration file was unknown");
            }

            authProvider.Configure(services);
            services.TryAddSingleton<IAuthenticationProvider>(authProvider);
        }
    }
}
