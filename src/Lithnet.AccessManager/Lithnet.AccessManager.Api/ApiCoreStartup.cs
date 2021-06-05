using DbUp.Engine.Output;
using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Text;

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

            // Dependencies

            services.AddSingleton<IUpgradeLog, DbUpgradeLogger>();
            services.AddSingleton<IDbProvider, SqlDbProvider>();
            services.AddSingleton<SqlLocalDbInstanceProvider>();
            services.AddSingleton<SqlServerInstanceProvider>();
            services.AddSingleton<RNGCryptoServiceProvider>();
            services.AddSingleton<IDirectory, ActiveDirectory>();
            services.AddSingleton<ICertificateProvider, CertificateProvider>();
            services.AddSingleton<IClusterProvider, ClusterProvider>();
            services.AddSingleton<IDiscoveryServices, DiscoveryServices>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton<IRegistryProvider>(new RegistryProvider(true));
            services.AddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();
            services.AddSingleton<IProtectedSecretProvider, ProtectedSecretProvider>();
            services.AddSingleton<IEncryptionProvider, EncryptionProvider>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton(RandomNumberGenerator.Create());

            // Our services

            services.AddScoped<IDeviceProvider, DbDeviceProvider>();
            services.AddScoped<IAadGraphApiProvider, AadGraphApiProvider>();
            services.AddScoped<IDbDevicePasswordProvider, DbDevicePasswordProvider>();
            services.AddSingleton<IApiErrorResponseProvider, ApiErrorResponseProvider>();
            services.AddSingleton<IAppPathProvider, ApiAppPathProvider>();
            services.AddSingleton<ISecurityTokenCache, SecurityTokenCache>();
            services.AddSingleton<ISecurityTokenGenerator, SecurityTokenGenerator>();
            services.AddSingleton<ISignedAssertionValidator, SignedAssertionValidator>();
            services.AddSingleton<RandomStringGenerator>();

            services.Configure<DatabaseConfigurationOptions>(this.Configuration.GetSection("DatabaseConfiguration"));
            services.Configure<LicensingOptions>(this.Configuration.GetSection("Licensing"));
            services.Configure<AzureAdOptions>(this.Configuration.GetSection("AzureAd"));
            services.Configure<PasswordPolicyOptions>(this.Configuration.GetSection("PasswordPolicy"));
            services.Configure<AgentOptions>(this.Configuration.GetSection("Agent"));
            services.Configure<TokenIssuerOptions>(this.Configuration.GetSection("TokenIssuer"));
            services.Configure<SignedAssertionValidationOptions>(this.Configuration.GetSection("TokenValidation"));
            services.Configure<DataProtectionOptions>(this.Configuration.GetSection("DataProtection"));


            IAmsLicenseManager licenseManager = this.CreateLicenseManager(services);
            services.AddSingleton(licenseManager);

            this.ConfigureAuthentication(services);
        }

        private void ConfigureAuthentication(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptions<TokenIssuerOptions>>().Value;
            var secretProvider = serviceProvider.GetRequiredService<IProtectedSecretProvider>();

            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretProvider.UnprotectSecret(options.SigningKey)));

            services.AddAuthentication()
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
                        ValidAudience = options.Audience,
                        RequireAudience = true,
                        ValidateIssuer = true,
                        ValidIssuer = options.Issuer,
                        ValidAlgorithms = new[] { options.SigningAlgorithm }
                    };
                });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("ComputersOnly", policy => policy.RequireClaim("object-type", "Computer"));
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
