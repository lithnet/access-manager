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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lithnet.AccessManager.Cryptography;
using Lithnet.AccessManager.Server.Authorization;

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
            services.AddSingleton<IActiveDirectory, ActiveDirectory>();
            services.AddSingleton<ICertificateProvider, CertificateProvider>();
            services.AddSingleton<IClusterProvider, ClusterProvider>();
            services.AddSingleton<IDiscoveryServices, DiscoveryServices>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton<IProtectedSecretProvider, ProtectedSecretProvider>();
            services.AddSingleton<IEncryptionProvider, EncryptionProvider>();
            services.AddSingleton<IWindowsServiceProvider, WindowsServiceProvider>();
            services.AddSingleton(RandomNumberGenerator.Create());
            services.AddSingleton<IRandomValueGenerator, RandomValueGenerator>();
            services.AddSingleton<IPasswordPolicyMemoryCache, PasswordPolicyMemoryCache>();
            // Our services

            services.AddScoped<IDeviceProvider, DbDeviceProvider>();
            services.AddScoped<IAadGraphApiProvider, AadGraphApiProvider>();
            services.AddScoped<IDevicePasswordProvider, DbDevicePasswordProvider>();
            services.AddScoped<IPasswordPolicyProvider, PasswordPolicyProvider>();
            services.AddScoped<IAmsGroupProvider, DbAmsGroupProvider>();

            services.AddSingleton<IRegistrationKeyProvider, DbRegistrationKeyProvider>();
            services.AddSingleton<ICheckInDataValidator, CheckInDataValidator>();
            services.AddSingleton<IApiErrorResponseProvider, ApiErrorResponseProvider>();
            services.AddSingleton<IAppPathProvider, ApiAppPathProvider>();
            services.AddSingleton<ISecurityTokenGenerator, SecurityTokenGenerator>();
            services.AddSingleton<ISignedAssertionValidator, SignedAssertionValidator>();

            services.Configure<AzureAdOptions>(this.Configuration.GetSection("AzureAd"));
            services.Configure<PasswordPolicyOptions>(this.Configuration.GetSection("PasswordPolicy"));
            services.Configure<ApiAuthenticationOptions>(this.Configuration.GetSection("ApiAuthentication"));
            services.Configure<TokenIssuerOptions>(this.Configuration.GetSection("TokenIssuer"));
            services.Configure<SignedAssertionValidationOptions>(this.Configuration.GetSection("TokenValidation"));
            services.Configure<DataProtectionOptions>(this.Configuration.GetSection("DataProtection"));
            services.Configure<HostingOptions>(Configuration.GetSection("Hosting"));

            this.ConfigureAuthentication(services);
        }

        private void ConfigureAuthentication(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var tokenIssuerOptions = serviceProvider.GetRequiredService<IOptions<TokenIssuerOptions>>().Value;
            var secretProvider = serviceProvider.GetRequiredService<IProtectedSecretProvider>();
            var hostingOptions = serviceProvider.GetRequiredService<IOptions<HostingOptions>>();

            if (tokenIssuerOptions.SigningKey == null)
            {
                throw new ConfigurationException("The API token signing key was not present");
            }

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
                o.AddPolicy(Constants.AuthZPolicyComputers, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("object-type", "Computer");
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });

                o.AddPolicy(Constants.AuthZPolicyAuthorityAzureAd, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("authority-type", AuthorityType.AzureActiveDirectory.ToString());
                });

                o.AddPolicy(Constants.AuthZPolicyAuthorityAms, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("authority-type", AuthorityType.Ams.ToString());
                });

                o.AddPolicy(Constants.AuthZPolicyAuthorityAd, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("authority-type", AuthorityType.ActiveDirectory.ToString());
                });

                o.AddPolicy(Constants.AuthZPolicyApprovedClient, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("approval-state", ApprovalState.Approved.ToString());
                });
            });
        }

        private bool InitializeLicenseManager(IServiceCollection services)
        {
            services.AddSingleton<ILicenseDataProvider, OptionsMonitorLicenseDataProvider>();
            services.AddSingleton<IRegistryProvider>(new RegistryProvider(true));
            services.Configure<LicensingOptions>(this.Configuration.GetSection("Licensing"));

            ServiceProvider provider = services.BuildServiceProvider();
            ILicenseDataProvider licenseDataProvider = provider.GetService<ILicenseDataProvider>();
            ILogger<AmsLicenseManager> licenseLogger = provider.GetService<ILogger<AmsLicenseManager>>();
            AmsLicenseManager licenseManager = new AmsLicenseManager(licenseLogger, licenseDataProvider);

            services.AddSingleton<IAmsLicenseManager>(licenseManager);

            return licenseManager.IsFeatureEnabled(LicensedFeatures.AmsApi);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAmsLicenseManager licenseManager, IOptions<ApiAuthenticationOptions> agentAuthOptions)
        {
            if (!licenseManager.IsFeatureEnabled(LicensedFeatures.AmsApi))
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiError(ApiConstants.NotLicensed, "The AMS server does not have a license to allow API use")));
                });
                return;
            }

            if (!(agentAuthOptions.Value.AllowAadAuth || agentAuthOptions.Value.AllowX509Auth))
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiError(ApiConstants.NoAuth, "The AMS server does not have any authentication modes enabled")));
                });
                return;
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.MapWhen(context => context.Request.Path.StartsWithSegments("/auth/iwa") && !agentAuthOptions.Value.AllowWindowsAuth,
                (IApplicationBuilder appBuilder) =>
                {
                    appBuilder.Run(async context =>
                    {
                        await Task.FromResult(context.Response.StatusCode = StatusCodes.Status403Forbidden);
                    });
                });

            app.MapWhen(context => context.Request.Path.StartsWithSegments("/auth/x509") && !agentAuthOptions.Value.AllowX509Auth,
                (IApplicationBuilder appBuilder) =>
                {
                    appBuilder.Run(async context =>
                    {
                        await Task.FromResult(context.Response.StatusCode = StatusCodes.Status403Forbidden);
                    });
                });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });


        }
    }
}