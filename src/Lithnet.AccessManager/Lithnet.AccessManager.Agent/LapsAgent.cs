using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class LapsAgent : ILapsAgent
    {
        private readonly ILogger<LapsAgent> logger;
        private readonly IActiveDirectoryLapsSettingsProvider adLapsSettings;
        private readonly IAgentSettings agentSettings;
        private readonly ActiveDirectoryLapsAgent activeDirectoryLapsAgent;
        private readonly AmsLapsAgent amsLapsAgent;

        private bool msLapsInstalled;

        public LapsAgent(ILogger<LapsAgent> logger, AmsLapsAgent advancedLapsAgent, IAgentSettings agentSettings)
        {
            this.logger = logger;
            this.amsLapsAgent = advancedLapsAgent;
            this.agentSettings = agentSettings;
        }

        public LapsAgent(ILogger<LapsAgent> logger,  ActiveDirectoryLapsAgent activeDirectoryLapsAgent, AmsLapsAgent advancedLapsAgent, IActiveDirectoryLapsSettingsProvider lapsSettings, IAgentSettings agentSettings) : this(logger, advancedLapsAgent, agentSettings)
        {
            this.adLapsSettings = lapsSettings;
            this.activeDirectoryLapsAgent = activeDirectoryLapsAgent;
        }

        public async Task DoCheck()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await this.DoCheckWindows();
                }
                else
                {
                    await this.DoCheckNonWindows();
                }

            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }

        private async Task DoCheckNonWindows()
        {
            if (this.agentSettings.AmsPasswordStorageEnabled && this.agentSettings.AmsServerManagementEnabled)
            {
                await this.amsLapsAgent.DoCheckAsync();
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
