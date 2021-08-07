using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Providers;
using Lithnet.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace Lithnet.AccessManager.Agent.MacOs
{
    public class Program
    {
        private static string confFilePath = "/Library/Application support/Lithnet/AccessManagerAgent/LithnetAccessManagerAgent.conf";
        private static string stateFilePath = "/Library/Application support/Lithnet/AccessManagerAgent/LithnetAccessManagerAgent.state";

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
                    services.AddTransient<IPasswordChangeProvider, MacOsPasswordChangeProvider>();
                    services.AddTransient<IAadJoinInformationProvider, MacOsAadJoinInformationProvider>();
                    services.AddTransient<ILapsAgent, MacOsLapsAgent>();
                    services.AddSingleton<IPlatformDataProvider, MacOsPlatformDataProvider>();
                    services.AddTransient<IAuthenticationCertificateProvider, MacOsAuthenticationCertificateProvider>();
                    services.AddSingleton<ICommandLineRunner, UnixCommandLineRunner>();
                    services.AddSingleton<IServiceController, MacOsServiceController>();
                    services.AddSingleton<IFilePathProvider, MacOsFilePathProvider>();
                    services.AddSingleton<ICommandLineArgumentProcessor, CommandLineArgumentProcessor>();

                    services.ConfigureWritable<AppState>(configuration.GetSection("State"), stateFilePath);
                    services.Configure<AgentOptions>(configuration.GetSection("Agent"));
                    services.Configure<UnixOptions>(configuration.GetSection("Unix"));


                    services.AddLogging(builder =>
                    {
                        if (File.Exists("NLog.config"))
                        {
                            builder.AddNLog("NLog.config");
                        }
                    });
                }
              ).UseLaunchd();
        }
    }
}