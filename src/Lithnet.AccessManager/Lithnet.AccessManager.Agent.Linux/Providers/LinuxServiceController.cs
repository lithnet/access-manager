using Microsoft.Extensions.Logging;
using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxServiceController : IServiceController
    {
        private readonly ICommandLineRunner cmdRunner;
        private readonly ILogger<LinuxServiceController> logger;

        public LinuxServiceController(ICommandLineRunner cmdLineRunner, ILogger<LinuxServiceController> logger)
        {
            this.cmdRunner = cmdLineRunner;
            this.logger = logger;
        }

        public void RestartService()
        {
            cmdRunner.ExecuteCommandWithDefaultShell("systemctl restart LithnetAccessManagerAgent.service").EnsureSuccess();
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
            cmdRunner.ExecuteCommandWithDefaultShell("systemctl daemon-reload").EnsureSuccess();
            cmdRunner.ExecuteCommandWithDefaultShell("systemctl enable LithnetAccessManagerAgent.service").EnsureSuccess();
            cmdRunner.ExecuteCommandWithDefaultShell("systemctl start LithnetAccessManagerAgent.service").EnsureSuccess();
        }
    }
}
