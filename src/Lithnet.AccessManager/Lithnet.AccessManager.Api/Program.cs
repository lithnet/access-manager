using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog.Web;

namespace Lithnet.AccessManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateDefaultHost(args).Build().Run();
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