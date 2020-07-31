using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
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
            var host = new HostBuilder();

            host.UseContentRoot(Directory.GetCurrentDirectory());

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
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsecrets.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("apphost.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables("AccessManagerService");
                
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            host.ConfigureLogging((hostingContext, logging) =>
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var env = hostingContext.HostingEnvironment;

                // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
                // the defaults be overridden by the configuration.
                if (isWindows)
                {
                    // Default the EventLogLoggerProvider to warning or above
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
                }

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                if (env.IsDevelopment())
                {
                    logging.AddConsole();
                    logging.AddDebug();
                }

                logging.AddEventSourceLogger();

                if (isWindows)
                {
                    // Add the EventLogLoggerProvider on windows machines
                    logging.AddEventLog(eventLogSettings =>
                    {
                        eventLogSettings.LogName = "Lithnet Access Manager";
                        eventLogSettings.SourceName = "Lithnet Access Manager Service";
                    });
                }
            });

            host.UseDefaultServiceProvider((context, options) =>
            {
                var isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            host.ConfigureWebHostDefaults(webBuilder =>
            {
                var httpsysConfig = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("apphost.json", optional: false)
                    .AddEnvironmentVariables("AccessManagerService")
                    .AddCommandLine(args)
                    .Build();

                webBuilder.UseHttpSys(httpsysConfig);
                webBuilder.UseStartup<Startup>();
            });

            host.UseWindowsService();

            return host;
        }
    }
}
