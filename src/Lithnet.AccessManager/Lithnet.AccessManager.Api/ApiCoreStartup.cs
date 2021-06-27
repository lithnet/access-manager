using DbUp.Engine.Output;
using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
            if (!this.InitializeLicenseManager(services))
            {
                return;
            }

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                });

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
            services.AddSingleton<IProtectedSecretProvider, ProtectedSecretProvider>();
            services.AddSingleton<IEncryptionProvider, EncryptionProvider>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton(RandomNumberGenerator.Create());

            // Our services

            services.AddSingleton<ICheckInDataValidator, CheckInDataValidator>();
            services.AddScoped<IDeviceProvider, DbDeviceProvider>();
            services.AddScoped<IAadGraphApiProvider, AadGraphApiProvider>();
            services.AddScoped<IDbDevicePasswordProvider, DbDevicePasswordProvider>();

            services.AddSingleton<IRegistrationKeyProvider, RegistrationKeyProvider>();

            services.AddSingleton<IApiErrorResponseProvider, ApiErrorResponseProvider>();
            services.AddSingleton<IAppPathProvider, ApiAppPathProvider>();
            services.AddSingleton<ISecurityTokenGenerator, SecurityTokenGenerator>();
            services.AddSingleton<ISignedAssertionValidator, SignedAssertionValidator>();
            services.AddSingleton<RandomStringGenerator>();

            services.Configure<DatabaseConfigurationOptions>(this.Configuration.GetSection("DatabaseConfiguration"));
            services.Configure<AzureAdOptions>(this.Configuration.GetSection("AzureAd"));
            services.Configure<PasswordPolicyOptions>(this.Configuration.GetSection("PasswordPolicy"));
            services.Configure<AgentOptions>(this.Configuration.GetSection("Agent"));
            services.Configure<TokenIssuerOptions>(this.Configuration.GetSection("TokenIssuer"));
            services.Configure<SignedAssertionValidationOptions>(this.Configuration.GetSection("TokenValidation"));
            services.Configure<DataProtectionOptions>(this.Configuration.GetSection("DataProtection"));
            services.Configure<ApiOptions>(this.Configuration.GetSection("Api"));
            services.Configure<HostingOptions>(Configuration.GetSection("Hosting"));

            this.ConfigureAuthentication(services);
        }

        private void ConfigureAuthentication(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var tokenIssuerOptions = serviceProvider.GetRequiredService<IOptions<TokenIssuerOptions>>().Value;
            var secretProvider = serviceProvider.GetRequiredService<IProtectedSecretProvider>();
            var hostingOptions = serviceProvider.GetRequiredService<IOptions<HostingOptions>>();

            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretProvider.UnprotectSecret(tokenIssuerOptions.SigningKey)));

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
                        ValidAudience = hostingOptions.Value.HttpSys.BuildApiHostUrl(),
                        RequireAudience = true,
                        ValidateIssuer = true,
                        ValidIssuer = hostingOptions.Value.HttpSys.BuildApiHostUrl(),
                        ValidAlgorithms = new[] { tokenIssuerOptions.SigningAlgorithm }
                    };
                });

            services.AddAuthorization(o =>
            {
                o.AddPolicy("ComputersOnly", policy => policy.RequireClaim("object-type", "Computer"));
            });
        }

        private bool InitializeLicenseManager(IServiceCollection services)
        {
            services.AddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();
            services.Configure<LicensingOptions>(this.Configuration.GetSection("Licensing"));

            ServiceProvider provider = services.BuildServiceProvider();
            ILicenseDataProvider licenseDataProvider = provider.GetService<ILicenseDataProvider>();
            ILogger<AmsLicenseManager> licenseLogger = provider.GetService<ILogger<AmsLicenseManager>>();
            AmsLicenseManager licenseManager = new AmsLicenseManager(licenseLogger, licenseDataProvider);

            services.AddSingleton<IAmsLicenseManager>(licenseManager);

            return licenseManager.IsEnterpriseEdition();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAmsLicenseManager licenseManager)
        {
            if (!licenseManager.IsEnterpriseEdition())
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiError("not-licensed", "The AMS server does not have a license to allow API use")));
                });
                return;
            }

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
