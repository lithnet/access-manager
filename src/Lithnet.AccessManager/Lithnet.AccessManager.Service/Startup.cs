using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Channels;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.Workers;
using Lithnet.AccessManager.Service.AppSettings;
using Lithnet.AccessManager.Service.Extensions;
using Lithnet.AccessManager.Service.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using Quartz;

namespace Lithnet.AccessManager.Service
{
    public class Startup
    {
        private IRegistryProvider registryProvider;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.registryProvider = new RegistryProvider(false);
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
            services.TryAddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.TryAddScoped<ITemplateProvider, TemplateProvider>();
            services.TryAddScoped<IJitAccessProvider, JitAccessProvider>();
            services.TryAddScoped<IPhoneticPasswordTextProvider, PhoneticStringProvider>();
            services.TryAddScoped<IHtmlPasswordProvider, HtmlPasswordProvider>();
            services.TryAddScoped<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>();
            services.TryAddScoped<IPasswordProvider, PasswordProvider>();
            services.TryAddScoped<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
            services.TryAddScoped<IAuthorizationInformationBuilder, AuthorizationInformationBuilder>();
            services.TryAddScoped<ITargetDataProvider, TargetDataProvider>();
            services.TryAddScoped<IBitLockerRecoveryPasswordProvider, BitLockerRecoveryPasswordProvider>();
            services.TryAddScoped<IComputerTargetProvider, ComputerTargetProvider>();

            services.TryAddSingleton<IDirectory, ActiveDirectory>();
            services.TryAddSingleton<IDiscoveryServices, DiscoveryServices>();
            services.TryAddSingleton<IEncryptionProvider, EncryptionProvider>();
            services.TryAddSingleton<ICertificateProvider, CertificateProvider>();
            services.TryAddSingleton<IAppPathProvider, WebAppPathProvider>();
            services.TryAddSingleton(RandomNumberGenerator.Create());
            services.TryAddSingleton<IJitAccessGroupResolver, JitAccessGroupResolver>();
            services.TryAddSingleton<IPowerShellSessionProvider, CachedPowerShellSessionProvider>();
            services.TryAddSingleton<IAuthorizationInformationMemoryCache, AuthorizationInformationMemoryCache>();
            services.TryAddSingleton<ITargetDataCache, TargetDataCache>();
            services.TryAddSingleton<IAuthorizationContextProvider, AuthorizationContextProvider>();
            services.TryAddSingleton<IClusterProvider, ClusterProvider>();
            services.TryAddSingleton<IProductSettingsProvider, ProductSettingsProvider>();
            services.TryAddSingleton<IProtectedSecretProvider, ProtectedSecretProvider>();
            services.TryAddSingleton<IRegistryProvider>(new RegistryProvider(false));
            services.TryAddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();
            services.TryAddSingleton<ICertificateSynchronizationProvider, CertificateSynchronizationProvider>();
            services.TryAddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.TryAddSingleton<ICertificatePermissionProvider, CertificatePermissionProvider>();
            services.TryAddSingleton<ILocalSam, LocalSam>();
            services.TryAddSingleton<IUpgradeLog, DbUpgradeLogger>();
            services.TryAddSingleton<IDbProvider, SqlDbProvider>();
            services.TryAddSingleton<SqlLocalDbInstanceProvider>();
            services.TryAddSingleton<SqlServerInstanceProvider>();

            services.AddOptions<QuartzOptions>()
                .Configure<IDbProvider>(
                (o, s) =>
                {
                    o.Add("quartz.dataSource.mydb.connectionString", s.ConnectionString);
                    o.Add("quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz");
                    o.Add("quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz");
                    o.Add("quartz.dataSource.mydb.provider", "SqlServer");
                    o.Add("quartz.jobStore.dataSource", "mydb");
                    o.Add("quartz.serializer.type", "json");
                    o.Add("quartz.scheduler.instanceName", "AMSCoreScheduler");
                    o.Add("quartz.scheduler.instanceId", "AUTO");
                });

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory(options =>
                {
                    options.AllowDefaultConstructor = true;
                });
            });

            services.AddScoped<INotificationChannel, SmtpNotificationChannel>();
            services.AddScoped<INotificationChannel, WebhookNotificationChannel>();
            services.AddScoped<INotificationChannel, PowershellNotificationChannel>();

            var backgroundProcessingChannel = Channel.CreateUnbounded<Action>();

            services.AddSingleton(backgroundProcessingChannel.Reader);
            services.AddSingleton(backgroundProcessingChannel.Writer);
            services.AddHostedService<AuditWorker>();
            services.AddHostedService<JitGroupWorker>();
            services.AddHostedService<CertificateImportWorker>();
            services.AddHostedService<SchedulerService>();

            if (registryProvider.CacheMode == 0)
            {
                services.TryAddScoped<IRateLimiter, SqlCacheRateLimiter>();
            }
            else
            {
                services.TryAddScoped<IRateLimiter, MemoryCacheRateLimiter>();
            }

            services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));

            services.Configure<UserInterfaceOptions>(Configuration.GetSection("UserInterface"));
            services.Configure<ConfigurationMetadata>(Configuration.GetSection("Metadata"));
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
            services.Configure<LicensingOptions>(Configuration.GetSection("Licensing"));
            services.Configure<DatabaseConfigurationOptions>(Configuration.GetSection("DatabaseConfiguration"));
            services.Configure<Server.Configuration.DataProtectionOptions>(Configuration.GetSection("DataProtection"));

            ILicenseManager licenseManager = this.CreateLicenseManager(services);

            services.AddSingleton(licenseManager);

            
            this.ConfigureAuthentication(services);
            this.ConfigureAuthorization(services);
            this.ConfigureDataProtection(services, licenseManager);
        }

        private ILicenseManager CreateLicenseManager(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var licenseDataProvider = provider.GetService<ILicenseDataProvider>();
            var licenseLogger = provider.GetService<ILogger<LicenseManager>>();
            var logger = provider.GetService<ILogger<Startup>>();
            LicenseManager licenseManager = new LicenseManager(licenseLogger, licenseDataProvider);

            try
            {
                var license = licenseManager.GetLicense();
                if (license != null)
                {
                    logger.LogTrace("License information\r\n{licenseData}", license.ToString());
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred performing the license check. Enterprise edition features will not be available");
            }

            if (licenseManager.IsEnterpriseEdition())
            {
                logger.LogInformation("Starting Lithnet Access Manager Enterprise Edition");
            }
            else
            {
                logger.LogInformation("Starting Lithnet Access Manager Standard Edition");
            }

            return licenseManager;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<ForwardedHeadersAppOptions> fwdOptions, IOptions<ConfigurationMetadata> metadata)
        {
            metadata.Value.ValidateMetadata();

            app.UseForwardedHeaders(fwdOptions.Value.ToNativeOptions());
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

        private void ConfigureDataProtection(IServiceCollection services, ILicenseManager licenseManager)
        {
            var provider = services.BuildServiceProvider();
            var dataProtectionOptions = provider.GetService<IOptions<Server.Configuration.DataProtectionOptions>>();

            IDataProtectionBuilder builder = services.AddDataProtection(options =>
            {
                options.ApplicationDiscriminator = "lithnetams";
            });

            SecurityIdentifier sid = WindowsIdentity.GetCurrent().User;

            RegistryKey key = Registry.LocalMachine.CreateSubKey($"Software\\Lithnet\\Access Manager Service\\Parameters\\Keys\\{sid}");
            builder.PersistKeysToRegistry(key);

            if (dataProtectionOptions.Value.EnableClusterCompatibleSecretEncryption && licenseManager.IsFeatureEnabled(LicensedFeatures.DpapiNgSecretEncryption))
            {
                if (dataProtectionOptions.Value.EnableClusterCompatibleSecretEncryption && licenseManager.IsFeatureEnabled(LicensedFeatures.DpapiNgSecretEncryption))
                {
                    builder.ProtectKeysWithDpapiNG($"SID={sid}", Microsoft.AspNetCore.DataProtection.XmlEncryption.DpapiNGProtectionDescriptorFlags.None);
                }
                else
                {
                    builder.ProtectKeysWithDpapi(false);
                }
            }
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
            services.TryAddSingleton(authProvider);
        }

        private void ConfigureAuthorization(IServiceCollection services)
        {
            var authSettings = Configuration.GetSection("Authentication").Get<AuthenticationOptions>();

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
