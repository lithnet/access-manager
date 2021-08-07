using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Linux.Configuration;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace Lithnet.AccessManager.Agent.Linux
{
    public class Program
    {
        private static string confFilePath = "/etc/LithnetAccessManagerAgent.conf";
        private static string stateFilePath = "/var/lib/LithnetAccessManagerAgent/LithnetAccessManagerAgent.state";

        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            if (args != null && args.Length > 0)
            {
                var processor = host.Services.GetRequiredService<ICommandLineArgumentProcessor>();
                processor.ProcessCommandLineArgs(args);
            }
            else
            {
                host.Run();
            }
        }

        [DebuggerStepThrough]
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile(confFilePath, optional: true, reloadOnChange: true);
                    config.AddJsonFile(stateFilePath, optional: true, reloadOnChange: true);
                })
                .ConfigureAccessManagerAgent()
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    services.AddSingleton<IAgentSettings, JsonFileSettingsProvider>();
                    services.AddTransient<IPasswordChangeProvider, LinuxPasswordChangeProvider>();
                    services.AddTransient<IAadJoinInformationProvider, LinuxAadJoinInformationProvider>();
                    services.AddTransient<ILapsAgent, LinuxLapsAgent>();
                    services.AddSingleton<IPlatformDataProvider, LinuxPlatformDataProvider>();
                    services.AddTransient<IAuthenticationCertificateProvider, LinuxAuthenticationCertificateProvider>();
                    services.AddSingleton<ICommandLineRunner, UnixCommandLineRunner>();
                    services.AddSingleton<IFilePathProvider, LinuxFilePathProvider>();
                    services.AddSingleton<IServiceController, LinuxServiceController>();
                    services.AddSingleton<ICommandLineArgumentProcessor, CommandLineArgumentProcessor>();

                    services.ConfigureWritable<AppState>(configuration.GetSection("State"), stateFilePath);
                    services.Configure<AgentOptions>(configuration.GetSection("Agent"));
                    services.Configure<LinuxOptions>(configuration.GetSection("Linux"));
                    services.Configure<UnixOptions>(configuration.GetSection("Unix"));

                    services.AddLogging(builder =>
                    {
                        if (File.Exists("NLog.config"))
                        {
                            builder.AddNLog("NLog.config");
                        }
                    });
                }
              )
              .UseSystemd();
        }
    }
}