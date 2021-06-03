using DbUp.Engine.Output;
using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class ApiCoreStartup
    {
        public ApiCoreStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IUpgradeLog, DbUpgradeLogger>();
            services.AddSingleton<IDbProvider, SqlDbProvider>();
            services.AddSingleton<SqlLocalDbInstanceProvider>();
            services.AddSingleton<SqlServerInstanceProvider>();
            services.AddSingleton<RNGCryptoServiceProvider>();
            services.AddSingleton<ISecurityTokenCache, SecurityTokenCache>();
            services.AddSingleton<RandomStringGenerator>();
            services.AddSingleton<IReplayNonceProvider, InMemoryReplayNonceProvider>();
            services.AddSingleton<ISecurityTokenGenerator, SecurityTokenGenerator>();
            services.AddSingleton<IDirectory, ActiveDirectory>();
            services.AddSingleton<ICertificateProvider, CertificateProvider>();

            services.AddSingleton<IClusterProvider, ClusterProvider>();
            services.AddSingleton<IDiscoveryServices, DiscoveryServices>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton<IRegistryProvider>(new RegistryProvider(true));
            services.AddSingleton<IAppPathProvider, ApiAppPathProvider>();
            services.AddSingleton<ISelfSignedAssertionValidator, SelfSignedAssertionValidator>();
            services.AddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();

            services.AddScoped<IDeviceProvider, DbDeviceProvider>();
            services.AddScoped<IAadGraphApiProvider, AadGraphApiProvider>();
            services.AddScoped<IDbDevicePasswordProvider, DbDevicePasswordProvider>();

            services.Configure<DatabaseConfigurationOptions>(this.Configuration.GetSection("DatabaseConfiguration"));
            services.Configure<LicensingOptions>(this.Configuration.GetSection("Licensing"));
            services.Configure<AzureAdOptions>(this.Configuration.GetSection("AzureAd"));
            services.Configure<PasswordPolicyOptions>(this.Configuration.GetSection("PasswordPolicy"));


            IAmsLicenseManager licenseManager = this.CreateLicenseManager(services);
            services.AddSingleton(licenseManager);


            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupers3cr3tsharedkey!"));

            services.AddAuthentication()
                .AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, o =>
            {
                o.AllowedCertificateTypes = CertificateTypes.All;
                o.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
                o.RevocationMode = X509RevocationMode.NoCheck;
                o.ValidateCertificateUse = false;
                o.ValidateValidityPeriod = false;

                o.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    }
                };

            })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = sharedKey,
                        RequireSignedTokens = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ValidateAudience = true,
                        ValidAudience = "api://default",
                        ValidateIssuer = true,
                        ValidIssuer = "https://{yourOktaDomain}/oauth2/default",
                        // ValidAlgorithms = new [] {"PS512", "PS256"},
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ComputersOnly", policy => policy.RequireClaim("object-type", "Computer", "User"));
            });
        }

        private IAmsLicenseManager CreateLicenseManager(IServiceCollection services)
        {
            ServiceProvider provider = services.BuildServiceProvider();
            ILicenseDataProvider licenseDataProvider = provider.GetService<ILicenseDataProvider>();
            ILogger<AmsLicenseManager> licenseLogger = provider.GetService<ILogger<AmsLicenseManager>>();
            ILogger<ApiCoreStartup> logger = provider.GetService<ILogger<ApiCoreStartup>>();
            AmsLicenseManager licenseManager = new AmsLicenseManager(licenseLogger, licenseDataProvider);

            try
            {
                ILicenseData license = licenseManager.GetLicense();
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


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
