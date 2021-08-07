using Microsoft.Extensions.Logging;
using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class MacOsServiceController : IServiceController
    {
        private readonly ICommandLineRunner cmdLineRunner;
        private readonly ILogger<MacOsServiceController> logger;

        public MacOsServiceController(ICommandLineRunner cmdLineRunner, ILogger<MacOsServiceController> logger)
        {
            this.cmdLineRunner = cmdLineRunner;
            this.logger = logger;
        }

        public void RestartService()
        {
            cmdLineRunner.ExecuteCommand("/bin/launchctl", "kickstart -k system/io.lithnet.accessmanager.agent").EnsureSuccess();
        }

        public void InstallService()
        {
            Console.WriteLine("Installing service");

            try
            {
                EnableService();
                Console.WriteLine("Done");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to install service");
                Console.WriteLine($"Unable to install service: {ex.Message}");
            }
        }

        public void EnableService()
        {
            cmdLineRunner.ExecuteCommand("/bin/launchctl", "load /Library/LaunchDaemons/io.lithnet.accessmanager.agent.plist").EnsureSuccess();
        }
    }
}
