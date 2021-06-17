using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Api
{
    internal static class HostBuilderExtensions
    {
        public static IConfigurationBuilder ConfigureAppSettings(this IConfigurationBuilder config)
        {
            RegistryProvider registryProvider = new RegistryProvider(false);

            string basePath = registryProvider.BasePath;
            string configPath = registryProvider.ConfigPath;

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                config.AddJsonFile(Path.Combine(configPath, "appsettings.json"), optional: false, reloadOnChange: true);
                config.AddJsonFile(Path.Combine(configPath, "appsecrets.json"), optional: true, reloadOnChange: true);
                config.AddJsonFile(Path.Combine(configPath, "apphost.json"), optional: true, reloadOnChange: true);
            }
            else if (!string.IsNullOrEmpty(basePath))
            {
                config.AddJsonFile(Path.Combine(basePath, "config\\appsettings.json"), optional: false, reloadOnChange: true);
                config.AddJsonFile(Path.Combine(basePath, "config\\appsecrets.json"), optional: true, reloadOnChange: true);
                config.AddJsonFile(Path.Combine(basePath, "config\\apphost.json"), optional: true, reloadOnChange: true);
            }
            else
            {
                config.AddJsonFile("config\\appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("config\\appsecrets.json", optional: true, reloadOnChange: true);
                config.AddJsonFile("config\\apphost.json", optional: false, reloadOnChange: true);
            }

            config.AddEnvironmentVariables("AccessManagerService");

            return config;
        }


        public static IHostBuilder ConfigureAccessManagerLogging(this IHostBuilder host)
        {
            return host.ConfigureLogging((hostingContext, logging) =>
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
        }

        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder builder, IConfiguration config)
        {
            HttpSysHostingOptions p = new HttpSysHostingOptions();
            config.Bind("Hosting:HttpSys", p);

            builder.UseHttpSys(options =>
            {
                options.Authentication.Schemes =
                    Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.Negotiate | Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.NTLM;
                options.Authentication.AllowAnonymous = true;
                options.ClientCertificateMethod = Microsoft.AspNetCore.Server.HttpSys.ClientCertificateMethod.NoCertificate;

                options.AllowSynchronousIO = p.AllowSynchronousIO;
                options.EnableResponseCaching = p.EnableResponseCaching;
                options.Http503Verbosity = (Microsoft.AspNetCore.Server.HttpSys.Http503VerbosityLevel)p.Http503Verbosity;
                options.MaxAccepts = p.MaxAccepts;
                options.MaxConnections = p.MaxConnections;
                options.MaxRequestBodySize = p.MaxRequestBodySize;
                options.RequestQueueLimit = p.RequestQueueLimit;
                options.ThrowWriteExceptions = p.ThrowWriteExceptions;

                p.Path = "api/v1.0";
                options.UrlPrefixes.Clear();
                options.UrlPrefixes.Add(p.BuildHttpsUrlPrefix());
            });

            return builder;
        }
    }
}
