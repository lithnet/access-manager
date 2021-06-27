using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;

namespace Lithnet.AccessManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RegistryProvider registryProvider = new RegistryProvider(false);
            if (!registryProvider.ApiEnabled)
            {
                BuildUnconfiguredHost().Build().Run();
            }
            else
            {
                CreateDefaultHost(args).Build().Run();
            }
        }

        private static IHostBuilder BuildUnconfiguredHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DisabledHost>();
                })
                .UseWindowsService()
                .ConfigureAccessManagerLogging();
        }

        public static IHostBuilder CreateDefaultHost(string[] args) =>

            Host
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
                })
                .UseWindowsService();
    }
}