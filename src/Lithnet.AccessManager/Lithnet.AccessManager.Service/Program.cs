using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Service.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;

[assembly: InternalsVisibleTo("Lithnet.AccessManager.Test")]
namespace Lithnet.AccessManager.Service
{
    public class Program
    {
        private static RegistryProvider registryProvider = new RegistryProvider(false);

        public static async Task Main(string[] args)
        {
            SetupNLog(registryProvider);

            if (args != null && args.Length > 0 && args[0] == "setup")
            {
                Setup.Process(args);
                return;
            }

            await Task.WhenAll(GetIHosts(args).Select(t => t.RunAsync()));
        }

        public static IEnumerable<IHost> GetIHosts(string[] args)
        {
            SetupNLog(registryProvider);

            bool safeStart = args?.Any(t => string.Equals(t, "/safeStart", StringComparison.OrdinalIgnoreCase)) ?? false;

            if (safeStart || !registryProvider.IsConfigured)
            {
                yield return Program.BuildUnconfiguredHost().UseWindowsService().Build();
                yield break;
            }

            var webHostBuilder = CreateWebHost(args).UseWindowsService();
            var webHost = webHostBuilder.Build();
            yield return webHost;

            if (registryProvider.ApiEnabled)
            {
                // Extract the WindowsServiceLifetime manager from the web host 
                var lifetime = webHost.Services.GetRequiredService<IHostApplicationLifetime>();

                yield return Api.Program.GetHostBuilder(args).ConfigureServices((builder) =>
                { 
                    // and inject it into the API host. The last HostApplicationLifetime injected is the one that is used by the framework
                    builder.AddSingleton<IHostApplicationLifetime>(lifetime);
                }).Build();
            }
        }

        private static IHostBuilder BuildUnconfiguredHost()
        {
            return Host.CreateDefaultBuilder().ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<UnconfiguredHost>();
                })
                .ConfigureAccessManagerLogging();
        }

        private static IHostBuilder CreateWebHost(string[] args)
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

            return host;
        }

        private static void SetupNLog(RegistryProvider registryProvider)
        {
            var configuration = new NLog.Config.LoggingConfiguration();

            var jitWorkerLog = new NLog.Targets.FileTarget("access-manager-jitworker")
            {
                FileName = Path.Combine(registryProvider.LogPath, "access-manager-jit-worker.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = registryProvider.RetentionDays,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            var serviceLog = new NLog.Targets.FileTarget("access-manager-service")
            {
                FileName = Path.Combine(registryProvider.LogPath, "access-manager-service.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = registryProvider.RetentionDays,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${aspnet-request-ip}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, jitWorkerLog, "Lithnet.AccessManager.Server.Workers.JitGroupWorker", true);
            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, serviceLog);

            NLog.LogManager.Configuration = configuration;
        }
    }
}
