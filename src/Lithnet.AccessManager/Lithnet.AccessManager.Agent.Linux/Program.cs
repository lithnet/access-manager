using System;
using Lithnet.AccessManager.Agent.Configuration;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Lithnet.AccessManager.Agent.Linux.Configuration;
using Lithnet.AccessManager.Agent.Shared;

namespace Lithnet.AccessManager.Agent.Linux
{
    public class Program
    {
        private static string confFilePath = "/etc/LithnetAccessManagerAgent.conf";
        private static string stateFilePath = "/var/lib/LithnetAccessManagerAgent/LithnetAccessManagerAgent.state";

        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                ProcessCommandLineArgs(args);
            }
            else
            {
                CreateHostBuilder(args).Build().Run();
            }
        }

        private static void ProcessCommandLineArgs(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return;
            }

            string serverName = null;
            string registrationKey = null;
            bool? enabled = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--server")
                {
                    if (args.Length > i + 1)
                    {
                        serverName = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("A server name is required");
                        return;
                    }

                    continue;
                }

                if (args[i] == "--install")
                {
                    InstallService();
                    return;
                }

                if (args[i] == "--setup")
                {
                    PromptForConfig();
                    return;
                }

                if (args[i] == "--registration-key")
                {
                    if (args.Length > i + 1)
                    {
                        registrationKey = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("A registration key is required");
                        return;
                    }

                    continue;
                }

                if (args[i] == "--disable")
                {
                    enabled = false;
                    continue;
                }

                if (args[i] == "--enable")
                {
                    enabled = true;
                    continue;
                }

                if (args[i] == "--help" || args[i] == "-h" || args[i] == "-?")
                {
                    Console.WriteLine($"Lithnet Access Manager Agent v{Assembly.GetEntryAssembly()?.GetName().Version}");
                    Console.WriteLine();
                    Console.WriteLine("Perform setup using interactive setup");
                    Console.WriteLine("--setup                         Use interactive setup");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Perform setup without user interaction");
                    Console.WriteLine("--server <servername>           Set the hostname of the AMS server");
                    Console.WriteLine("--registration-key <key>        Set the registration key");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Control the agent state");
                    Console.WriteLine("--disable                       Disable the agent");
                    Console.WriteLine("--enable                        Enable the agent");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Install the agent as a service (non-packaged installs only)");
                    Console.WriteLine("--install                       Installs the agent - use only when not using");
                    Console.WriteLine("                                a package installer (eg from tar.gz)");
                    return;
                }

                Console.WriteLine($"Unknown argument {args[i]}");
                return;
            }

            if (serverName == null && registrationKey == null && enabled == null)
            {
                return;
            }

            Program.WriteConfig(serverName, enabled, registrationKey);
        }

        private static void PromptForConfig()
        {
            string server;
            string regKey;

            do
            {
                Console.WriteLine("Enter the fully qualified DNS name of the Access Manager server (eg ams.lithnet.io):");
                server = Console.ReadLine();
            }
            while (string.IsNullOrWhiteSpace(server));

            do
            {
                Console.WriteLine("Enter the registration key:");
                regKey = Console.ReadLine();
            }
            while (string.IsNullOrWhiteSpace(regKey));

            WriteConfig(server, null, regKey);
        }

        private static void WriteConfig(string serverName, bool? enabled, string registrationKey, bool restart = true)
        {
            AppConfigFile appConfig = null;

            if (File.Exists(Program.confFilePath))
            {
                appConfig = JsonSerializer.Deserialize<AppConfigFile>(File.ReadAllText(Program.confFilePath), AgentJsonSettings.JsonSerializerDefaults);
            }

            appConfig ??= new AppConfigFile();

            appConfig.Agent.AmsPasswordStorageEnabled = true;
            appConfig.Agent.AuthenticationMode = Api.Shared.AgentAuthenticationMode.Ams;
            appConfig.Agent.AzureTenantIDs = null;

            if (!string.IsNullOrWhiteSpace(serverName))
            {
                appConfig.Agent.Server = serverName;
            }

            if (enabled != null)
            {
                appConfig.Agent.Enabled = enabled.Value;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Program.confFilePath));
            File.WriteAllText(Program.confFilePath, JsonSerializer.Serialize(appConfig, AgentJsonSettings.JsonSerializerDefaults));


            if (!string.IsNullOrWhiteSpace(registrationKey))
            {
                AppStateFile appState = null;

                if (File.Exists(Program.stateFilePath))
                {
                    appState = JsonSerializer.Deserialize<AppStateFile>(File.ReadAllText(Program.stateFilePath), AgentJsonSettings.JsonSerializerDefaults);
                }

                appState ??= new AppStateFile();

                appState.State.RegistrationKey = registrationKey;
                Directory.CreateDirectory(Path.GetDirectoryName(Program.stateFilePath));
                File.WriteAllText(Program.stateFilePath, JsonSerializer.Serialize(appState, AgentJsonSettings.JsonSerializerDefaults));
            }

            Console.WriteLine("Configuration updated");

            if (restart)
            {
                if (Program.TryRestartService())
                {
                    Console.WriteLine("The service has been restarted");
                    return;
                }

                Console.WriteLine("The service restart command did not succeed");
            }

            Console.WriteLine("Restart the service with 'systemctl restart LithnetAccessManagerAgent' for the new settings to take effect");
        }

        private static bool TryRestartService()
        {
            LinuxCommandLineRunner cmdRunner = new LinuxCommandLineRunner();

            try
            {
                cmdRunner.ExecuteCommandWithShell("systemctl restart LithnetAccessManagerAgent.service");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to restart service: {ex.Message}");
            }

            return false;
        }

        private static void InstallService()
        {
            Console.WriteLine("Reloading systemctl and starting service");
            try
            {
                TryEnableService();
                Console.WriteLine("Done");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to install service: {ex.Message}");
            }
        }

        private static void TryEnableService()
        {
            LinuxCommandLineRunner cmdRunner = new LinuxCommandLineRunner();

            cmdRunner.ExecuteCommandWithShell("systemctl daemon-reload");
            cmdRunner.ExecuteCommandWithShell("systemctl enable LithnetAccessManagerAgent.service");
            cmdRunner.ExecuteCommandWithShell("systemctl start LithnetAccessManagerAgent.service");
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

                    services.ConfigureWritable<AppState>(configuration.GetSection("State"), stateFilePath);
                    services.Configure<AgentOptions>(configuration.GetSection("Agent"));
                    services.Configure<LinuxOptions>(configuration.GetSection("Linux"));

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