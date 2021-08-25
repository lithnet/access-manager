using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Channels;
using DbUp.Engine.Output;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Cryptography;
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
using Lithnet.Licensing.Core;
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
            this.registryProvider = new RegistryProvider(true);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddResponseCaching();

            services.AddHsts(options =>
            {
                options.Preload = false;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Unspecified;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            });

            services.AddScoped<IIwaAuthenticationProvider, IwaAuthenticationProvider>();
            services.AddScoped<IOidcAuthenticationProvider, OidcAuthenticationProvider>();
            services.AddScoped<IWsFedAuthenticationProvider, WsFedAuthenticationProvider>();
            services.AddScoped<ICertificateAuthenticationProvider, CertificateAuthenticationProvider>();

            services.AddScoped<IAuthorizationService, SecurityDescriptorAuthorizationService>();
            services.AddScoped<SecurityDescriptorAuthorizationService>();
            services.AddScoped<IPowerShellSecurityDescriptorGenerator, PowerShellSecurityDescriptorGenerator>();
            services.AddScoped<IAuditEventProcessor, AuditEventProcessor>();
            services.AddScoped<ITemplateProvider, TemplateProvider>();
            services.AddScoped<IJitAccessProvider, JitAccessProvider>();
            services.AddScoped<IPhoneticPasswordTextProvider, PhoneticStringProvider>();
            services.AddScoped<IHtmlPasswordProvider, HtmlPasswordProvider>();
            services.AddScoped<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>();
            services.AddScoped<IPasswordProvider, PasswordProvider>();
            services.AddScoped<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
            services.AddScoped<IAuthorizationInformationBuilder, AuthorizationInformationBuilder>();
            services.AddScoped<ITargetDataProvider, TargetDataProvider>();
            services.AddScoped<IBitLockerRecoveryPasswordProvider, BitLockerRecoveryPasswordProvider>();

            services.AddScoped<IComputerTargetProvider, ComputerTargetProviderAd>();
            services.AddScoped<IComputerTargetProvider, ComputerTargetProviderAzureAd>();
            services.AddScoped<IComputerTargetProvider, ComputerTargetProviderAms>();
            services.AddScoped<IComputerTokenSidProvider, ComputerTokenSidProvider>();

            services.AddSingleton<IComputerSearchResultProvider, ComputerSearchResultProvider>();
            services.AddSingleton<IComputerLocator, ComputerLocator>();
            services.AddSingleton<IAadGraphApiProvider, AadGraphApiProvider>();

            services.AddSingleton<IDeviceProvider, DbDeviceProvider>();
            services.AddSingleton<IDevicePasswordProvider, DbDevicePasswordProvider>();
            services.AddSingleton<IRegistrationKeyProvider, DbRegistrationKeyProvider>();
            services.AddSingleton<IAmsGroupProvider, AmsGroupProvider>();
            services.AddSingleton<IDbAmsGroupProvider, DbAmsGroupProvider>();
            services.AddSingleton<IAmsSystemGroupProvider, AmsSystemGroupProvider>();
            services.AddSingleton<IAuthorityDataProvider, AuthorityDataProvider>();
                
            services.AddSingleton<ISmtpProvider, SmtpProvider>();
            services.AddSingleton<IApplicationUpgradeProvider, ApplicationUpgradeProvider>();
            services.AddSingleton<IActiveDirectory, ActiveDirectory>();
            services.AddSingleton<IDiscoveryServices, DiscoveryServices>();
            services.AddSingleton<IEncryptionProvider, EncryptionProvider>();
            services.AddSingleton<ICertificateProvider, CertificateProvider>();
            services.AddSingleton<IAppPathProvider, WebAppPathProvider>();
            services.AddSingleton(RandomNumberGenerator.Create());
            services.AddSingleton<IRandomValueGenerator, RandomValueGenerator>();
            services.AddSingleton<IJitAccessGroupResolver, JitAccessGroupResolver>();
            services.AddSingleton<IPowerShellSessionProvider, CachedPowerShellSessionProvider>();
            services.AddSingleton<IAuthorizationInformationMemoryCache, AuthorizationInformationMemoryCache>();
            services.AddSingleton<ITargetDataCache, TargetDataCache>();
            services.AddSingleton<IAuthorizationContextProvider, AuthorizationContextProvider>();
            services.AddSingleton<IClusterProvider, ClusterProvider>();
            services.AddSingleton<IProductSettingsProvider, ProductSettingsProvider>();
            services.AddSingleton<IProtectedSecretProvider, ProtectedSecretProvider>();
            services.AddSingleton<IRegistryProvider>(new RegistryProvider(true));
            services.AddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();
            services.AddSingleton<ICertificateSynchronizationProvider, CertificateSynchronizationProvider>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton<ICertificatePermissionProvider, CertificatePermissionProvider>();
            services.AddSingleton<ILocalSam, LocalSam>();
            services.AddSingleton<IUpgradeLog, DbUpgradeLogger>();
            services.AddSingleton<IDbProvider, SqlDbProvider>();
            services.AddSingleton<SqlLocalDbInstanceProvider>();
            services.AddSingleton<SqlServerInstanceProvider>();
            services.AddSingleton<IHttpSysConfigurationProvider, HttpSysConfigurationProvider>();

            services.AddTransient<NewVersionCheckJob>();
            services.AddTransient<CertificateExpiryCheckJob>();
            services.AddTransient<DbBackupJob>();
            services.AddTransient<DbMaintenanceJob>();

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
                    o.Add("quartz.threadPool.threadCount", "10");
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
                services.AddScoped<IRateLimiter, SqlCacheRateLimiter>();
            }
            else
            {
                services.AddScoped<IRateLimiter, MemoryCacheRateLimiter>();
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
            services.Configure<Server.Configuration.DataProtectionOptions>(Configuration.GetSection("DataProtection"));
            services.Configure<AdminNotificationOptions>(Configuration.GetSection("AdminNotifications"));
            services.Configure<AzureAdOptions>(this.Configuration.GetSection("AzureAd"));
            services.Configure<DatabaseOptions>(this.Configuration.GetSection("Database"));

            IAmsLicenseManager licenseManager = this.CreateLicenseManager(services);

            services.AddSingleton(licenseManager);


            this.ConfigureAuthentication(services);
            this.ConfigureAuthorization(services);
            this.ConfigureDataProtection(services, licenseManager);
        }

        private IAmsLicenseManager CreateLicenseManager(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var licenseDataProvider = provider.GetService<ILicenseDataProvider>();
            var licenseLogger = provider.GetService<ILogger<AmsLicenseManager>>();
            var logger = provider.GetService<ILogger<Startup>>();
            AmsLicenseManager licenseManager = new AmsLicenseManager(licenseLogger, licenseDataProvider);

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
                logger.LogInformation("Starting Lithnet Access Manager Community Edition");
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
            app.UseCookiePolicy();
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

        private void ConfigureDataProtection(IServiceCollection services, IAmsLicenseManager licenseManager)
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
