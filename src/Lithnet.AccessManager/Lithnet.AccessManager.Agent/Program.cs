using Lithnet.AccessManager.Agent.Authentication;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Agent.Test")]

namespace Lithnet.AccessManager.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        [DebuggerStepThrough]
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appstate.json", optional: true, reloadOnChange: true);
                })

                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    services.AddHostedService<Worker>();
                    services.AddHttpClient(Constants.HttpClientAuthAnonymous, (serviceProvider, c) =>
                    {
                        c.DefaultRequestHeaders.Add("Accept", "application/json");
                        c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                        var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                        c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                    });

                    services.AddHttpClient(Constants.HttpClientAuthIwa, (serviceProvider, c) =>
                    {
                        c.DefaultRequestHeaders.Add("Accept", "application/json");
                        c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                        var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                        c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                    {
                        AllowAutoRedirect = false,
                        UseDefaultCredentials = true,
                    });

                    services.AddHttpClient(Constants.HttpClientAuthBearer, (serviceProvider, c) =>
                    {
                        c.DefaultRequestHeaders.Add("Accept", "application/json");
                        c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                        var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                        c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                    })
                    .AddHttpMessageHandler<BearerTokenHandler>();


                    services.AddSingleton(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        IgnoreNullValues = true,
                        PropertyNamingPolicy = null,
                    });

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Legacy LAPS provider services
                        services.AddTransient<ILocalSam, LocalSam>();
                        services.AddTransient<IDirectory, ActiveDirectory>();
                        services.AddTransient<IDiscoveryServices, DiscoveryServices>();
                        services.AddTransient<IPasswordChangeProvider, WindowsPasswordChangeProvider>();
                        services.AddTransient<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
                        services.AddTransient<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>();
                        services.AddTransient<ICertificateProvider, CertificateProvider>();
                        services.AddTransient<ActiveDirectoryLapsAgent>();
                        services.AddSingleton<IActiveDirectoryLapsSettingsProvider, ActiveDirectoryLapsSettingsProvider>();

                        // Advanced agent services
                        services.AddSingleton<IwaTokenProvider>();
                        services.AddSingleton<IAgentSettings, WindowsAgentSettingsProvider>();
                        services.AddSingleton<IRegistryPathProvider, RegistryPathProvider>();
                        services.AddSingleton<IAadJoinInformationProvider, WindowsAadJoinInformationProvider>();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        services.AddSingleton<IAgentSettings, JsonFileSettingsProvider>();

                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        services.AddSingleton<IAgentSettings, JsonFileSettingsProvider>();
                    }
                    else
                    {
                        throw new PlatformNotSupportedException();
                    }

                    services.AddSingleton<X509TokenProvider>();
                    services.AddSingleton<ITokenProvider, TokenProvider>();
                    services.AddTransient<BearerTokenHandler>();
                    services.AddSingleton<ITokenClaimProvider, TokenClaimProvider>();
                    services.AddTransient<IAgentCheckInProvider, AgentCheckInProvider>();
                    services.AddTransient<IPasswordStorageProvider, AmsApiPasswordStorageProvider>();
                    services.AddTransient<IAuthenticationCertificateProvider, AuthenticationCertificateProvider>();
                    services.AddSingleton<IMetadataProvider, MetadataProvider>();
                    services.AddTransient<IRegistrationProvider, RegistrationProvider>();

                    services.AddTransient<AmsLapsAgent>();
                    services.AddTransient<ILapsAgent, LapsAgent>();
                    services.AddTransient<IPasswordGenerator, RandomPasswordGenerator>();
                    services.AddSingleton(RandomNumberGenerator.Create());
                    services.AddSingleton<IRandomValueGenerator, RandomValueGenerator>();

                    services.AddTransient<IEncryptionProvider, EncryptionProvider>();
                    services.AddSingleton<IMetadataProvider, MetadataProvider>();
                    // config

                    services.ConfigureWritable<AppState>(configuration.GetSection("State"), "appstate.json");
                    services.Configure<AgentOptions>(configuration.GetSection("Agent"));

                    services.AddLogging(builder =>
                    {
                        builder.AddNLog("nlog.config");
                        builder.AddEventLog(settings =>
                        {
                            settings.LogName = "Lithnet Access Manager";
                            settings.SourceName = "Lithnet Access Manager Agent";
                            settings.Filter = (x, y) => y >= LogLevel.Information;
                        });
                    });
                }
              )
                .UseWindowsService()
                .UseSystemd();
        }
    }
}