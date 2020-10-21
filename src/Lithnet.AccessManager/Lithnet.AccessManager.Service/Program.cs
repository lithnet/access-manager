using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Lithnet.AccessManager.Service.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using NLog.LayoutRenderers;
using NLog.Web;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Test")]
namespace Lithnet.AccessManager.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetupNLog();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(Constants.BaseKey, false);
            bool safeStart = args.Any(t => string.Equals(t, "/safeStart", System.StringComparison.OrdinalIgnoreCase));

            if (safeStart || (!(baseKey?.GetValue("Configured", 0) is int value) || value == 0))
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

        private static void SetupNLog()
        {
            RegistryKey paramsKey = Registry.LocalMachine.OpenSubKey(Constants.ParametersKey, false);
            string logPath = paramsKey?.GetValue("LogPath") as string ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");
            int retentionDays = Math.Max(paramsKey?.GetValue("LogRetentionDays") as int? ?? 7, 1);

            var configuration = new NLog.Config.LoggingConfiguration();

            var jitWorkerLog = new NLog.Targets.FileTarget("access-manager-jitworker")
            {
                FileName = Path.Combine(logPath, "access-manager-jit-worker.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = retentionDays,
                Layout= "${longdate}|${level:uppercase=true:padding=5}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            var serviceLog = new NLog.Targets.FileTarget("access-manager-service")
            {
                FileName = Path.Combine(logPath, "access-manager-service.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = retentionDays,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, jitWorkerLog, "Lithnet.AccessManager.Server.Workers.JitGroupWorker", true);
            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, serviceLog);

            NLog.LogManager.Configuration = configuration;
        }
    }
}
