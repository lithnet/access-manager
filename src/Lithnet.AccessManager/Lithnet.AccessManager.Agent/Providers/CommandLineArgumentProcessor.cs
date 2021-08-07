using Lithnet.AccessManager.Agent.Shared;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class CommandLineArgumentProcessor : ICommandLineArgumentProcessor
    {
        private readonly string confFilePath;
        private readonly string stateFilePath;
        private readonly IServiceController serviceController;

        public CommandLineArgumentProcessor(IFilePathProvider pathProvider, IServiceController serviceController)
        {
            this.confFilePath = pathProvider.ConfFilePath;
            this.stateFilePath = pathProvider.StateFilePath;
            this.serviceController = serviceController;
        }

        public void ProcessCommandLineArgs(string[] args)
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
                    this.serviceController.InstallService();
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

            this.WriteConfig(serverName, enabled, registrationKey);
        }

        private void PromptForConfig()
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

        private void WriteConfig(string serverName, bool? enabled, string registrationKey, bool restart = true)
        {
            AppConfigFile appConfig = null;

            if (File.Exists(this.confFilePath))
            {
                appConfig = JsonSerializer.Deserialize<AppConfigFile>(File.ReadAllText(this.confFilePath), AgentJsonSettings.JsonSerializerDefaults);
            }

            if (appConfig == null)
            {
                appConfig = new AppConfigFile();
            }

            if (!string.IsNullOrWhiteSpace(serverName))
            {
                appConfig.Agent.Server = serverName;
            }

            if (enabled != null)
            {
                appConfig.Agent.Enabled = enabled.Value;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(this.confFilePath));
            File.WriteAllText(this.confFilePath, JsonSerializer.Serialize(appConfig, AgentJsonSettings.JsonSerializerDefaults));


            if (!string.IsNullOrWhiteSpace(registrationKey))
            {
                AppStateFile appState = null;

                if (File.Exists(this.stateFilePath))
                {
                    appState = JsonSerializer.Deserialize<AppStateFile>(File.ReadAllText(this.stateFilePath), AgentJsonSettings.JsonSerializerDefaults);
                }

                if (appState == null)
                {
                    appState = new AppStateFile();
                }

                appState.State.RegistrationState = RegistrationState.NotRegistered;
                appState.State.RegistrationKey = registrationKey;
                Directory.CreateDirectory(Path.GetDirectoryName(this.stateFilePath));
                File.WriteAllText(this.stateFilePath, JsonSerializer.Serialize(appState, AgentJsonSettings.JsonSerializerDefaults));
            }

            Console.WriteLine("Configuration updated");

            if (restart)
            {
                try
                {
                    this.serviceController.RestartService();
                    Console.WriteLine("The service has been restarted");
                    return;
                }
                catch
                {
                    Console.WriteLine("The service restart command did not succeed");
                }
            }

            Console.WriteLine("Restart the service for the new settings to take effect");
        }
    }
}
