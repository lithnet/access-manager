using Lithnet.AccessManager.Agent.Authentication;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Agent.Test")]

namespace Lithnet.AccessManager.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Thread.Sleep(5000);
            }

            CreateHostBuilder(args).Build().Run();
        }

        [DebuggerStepThrough]
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("/etc/lithnetaccessmanager.conf", optional: true, reloadOnChange: true);
                })
                .ConfigureAccessManagerAgent()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    // Windows specific services

                    services.AddSingleton<IRegistryPathProvider, RegistryPathProvider>();

                    // Legacy LAPS provider services
                    services.AddTransient<ILocalSam, LocalSam>();
                    services.AddTransient<IActiveDirectory, ActiveDirectory>();
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
                    services.AddSingleton<IAadJoinInformationProvider, WindowsAadJoinInformationProvider>();
                    services.AddTransient<ILapsAgent, WindowsLapsAgent>();
                    services.AddSingleton<IPlatformDataProvider, WindowsPlatformDataProvider>();
                    services.AddTransient<IAuthenticationCertificateProvider, WindowsAuthenticationCertificateProvider>();
                    services.AddTransient<ITokenClaimProvider, TokenClaimProviderAad>();

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
                .UseWindowsService();
        }
    }
}