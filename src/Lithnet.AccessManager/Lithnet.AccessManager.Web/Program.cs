using System.IO;
using System.Runtime.CompilerServices;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using NLog.Web;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Test")]
namespace Lithnet.AccessManager.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(Constants.BaseKey, false);

            if (!(key?.GetValue("Configured", 0) is int value) || value == 0)
            {
                return Host.CreateDefaultBuilder().ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<UnconfiguredHost>();
                    })
                    .UseWindowsService()
                    .ConfigureAccessManagerLogging();
            }

            var host = new HostBuilder();

            host.UseContentRoot(Directory.GetCurrentDirectory());

            string basePath = key?.GetValue("BasePath") as string;

            host.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            host.UseNLog();

            host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.ConfigureAppSettings();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            host.ConfigureAccessManagerLogging();

            host.UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            host.ConfigureWebHostDefaults(webBuilder =>
            {
                var httpsysConfig = new ConfigurationBuilder().ConfigureAppSettings().Build();
                    
                webBuilder.UseHttpSys(httpsysConfig);
                webBuilder.UseStartup<Startup>();
            });

            host.UseWindowsService();

            return host;
        }
    }
}
