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
        private readonly ISettingsProvider settings;
        private readonly ActiveDirectoryLapsAgent activeDirectoryLapsAgent;
        private readonly AmsLapsAgent amsLapsAgent;

        private bool msLapsInstalled;
        private bool isDisabledLogged;

        public LapsAgent(ILogger<LapsAgent> logger, ISettingsProvider settings, AmsLapsAgent advancedLapsAgent)
        {
            this.logger = logger;
            this.settings = settings;
            this.amsLapsAgent = advancedLapsAgent;
        }

        public LapsAgent(ILogger<LapsAgent> logger, ISettingsProvider settings, ActiveDirectoryLapsAgent legacyAgent, AmsLapsAgent advancedLapsAgent)
        {
            this.logger = logger;
            this.settings = settings;
            this.activeDirectoryLapsAgent = legacyAgent;
            this.amsLapsAgent = advancedLapsAgent;
        }

        public async Task DoCheck()
        {
            try
            {
                if (!this.settings.PasswordManagementEnabled)
                {
                    if (!this.isDisabledLogged)
                    {
                        this.logger.LogTrace(EventIDs.LapsAgentDisabled, "The local admin password agent is disabled");
                        this.isDisabledLogged = true;
                    }

                    return;
                }

                if (this.isDisabledLogged)
                {
                    this.logger.LogTrace(EventIDs.LapsAgentEnabled, "The local admin password agent is enabled");
                    this.isDisabledLogged = false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || this.settings.AdvancedAgentEnabled)
                {
                    await this.amsLapsAgent.DoCheckAsync();
                }
                else
                {
                    this.activeDirectoryLapsAgent.DoCheck();
                }

            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
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
