using System.IO;
using Lithnet.AccessManager.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;

namespace Lithnet.AccessManager.Api
{
    public class Program
    {
        private static RegistryProvider registryProvider = new RegistryProvider(false);

        public static void Main(string[] args)
        {
            SetupNLog(registryProvider);

            GetHostBuilder(args)
                .UseWindowsService()
                .Build()
                .Run();
        }

        public static IHostBuilder GetHostBuilder(string[] args)
        {
            if (!registryProvider.ApiEnabled)
            {
                return BuildUnconfiguredHost();
            }
            else
            {
                return CreateApiHost(args);
            }
        }

        private static IHostBuilder BuildUnconfiguredHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DisabledHost>();
                })
                .ConfigureAccessManagerLogging();
        }

        private static IHostBuilder CreateApiHost(string[] args)
        {

            return Host
                 .CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((hostingContext, config) =>
                 {
                     config.ConfigureAppSettings();

                     if (args != null)
                     {
                         config.AddCommandLine(args);
                     }
                 })
                 .UseNLog()
                 .ConfigureAccessManagerLogging()
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     var config = new ConfigurationBuilder().ConfigureAppSettings().Build();

                     webBuilder
                         .UseHttpSys(config)
                         .UseStartup<ApiCoreStartup>();
                 });
        }

        private static void SetupNLog(RegistryProvider registryProvider)
        {
            var configuration = new NLog.Config.LoggingConfiguration();

            var apiLog = new NLog.Targets.FileTarget("access-manager-api")
            {
                FileName = Path.Combine(registryProvider.LogPath, "access-manager-api.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = registryProvider.RetentionDays,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${aspnet-request-ip}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, apiLog);

            NLog.LogManager.Configuration = configuration;
        }
    }
}