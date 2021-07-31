using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class WindowsLapsAgent : ILapsAgent
    {
        private readonly ILogger<WindowsLapsAgent> logger;
        private readonly IActiveDirectoryLapsSettingsProvider adLapsSettings;
        private readonly IAgentSettings agentSettings;
        private readonly ActiveDirectoryLapsAgent activeDirectoryLapsAgent;
        private readonly AmsLapsAgent amsLapsAgent;
        private readonly ILocalSam localSam;
        private readonly IHostApplicationLifetime appLifetime;

        private bool msLapsInstalled;

        public WindowsLapsAgent(ILogger<WindowsLapsAgent> logger, ActiveDirectoryLapsAgent activeDirectoryLapsAgent, AmsLapsAgent advancedLapsAgent, IActiveDirectoryLapsSettingsProvider lapsSettings, IAgentSettings agentSettings, ILocalSam localSam, IHostApplicationLifetime appLifetime)
        {
            this.logger = logger;
            this.amsLapsAgent = advancedLapsAgent;
            this.agentSettings = agentSettings;
            this.localSam = localSam;
            this.appLifetime = appLifetime;
            this.adLapsSettings = lapsSettings;
            this.activeDirectoryLapsAgent = activeDirectoryLapsAgent;
        }

        public async Task DoCheck()
        {
            try
            {
                await this.DoCheckWindows();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }

        private async Task DoCheckWindows()
        {
            if (this.IsMsLapsInstalled())
            {
                if (!this.msLapsInstalled)
                {
                    this.logger.LogWarning(EventIDs.LapsConflict, "The Microsoft LAPS client is installed and enabled. Disable the Microsoft LAPS agent via group policy or uninstall it to allow this tool to manage the local administrator password");
                    this.msLapsInstalled = true;
                }

                return;
            }
            else
            {
                if (this.msLapsInstalled)
                {
                    this.msLapsInstalled = false;
                    this.logger.LogInformation(EventIDs.LapsConflictResolved, "The Microsoft LAPS client has been removed or disabled. Lithnet Access Manager will now set the local admin password for this machine");
                }
            }

            if (this.localSam.IsDomainController())
            {
                this.logger.LogWarning(EventIDs.RunningOnDC, "This application should not be run on a domain controller. Shutting down");
                this.appLifetime.StopApplication();
                return;
            }

            if (this.agentSettings.AmsPasswordStorageEnabled && this.agentSettings.AmsServerManagementEnabled)
            {
                await this.amsLapsAgent.DoCheckAsync();
                return;
            }

            if (this.adLapsSettings.Enabled)
            {
                this.activeDirectoryLapsAgent.DoCheck();
                return;
            }
        }

        private bool IsMsLapsInstalled()
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey r = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\77F1646A33805F848A7A683CFB6B88A7", false);

            if (r == null)
            {
                return false;
            }

            r = baseKey.OpenSubKey(@"SOFTWARE\Policies\Microsoft Services\AdmPwd", false);

            return r?.GetValue<int>("AdmPwdEnabled", 0) == 1;
        }
    }
}
