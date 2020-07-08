using System;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class LapsAgent : ILapsAgent
    {
        private readonly ILogger<LapsAgent> logger;

        private readonly IDirectory directory;

        private readonly ILapsSettings settings;

        private readonly IPasswordGenerator passwordGenerator;

        private readonly IEncryptionProvider encryptionProvider;

        private readonly ICertificateProvider certificateProvider;

        private readonly ILocalSam sam;

        private readonly IAppDataProvider appDataProvider;

        private readonly IMsMcsAdmPwdProvider msMcsAdmPwdProvider;

        public LapsAgent(ILogger<LapsAgent> logger, IDirectory directory, ILapsSettings settings, IPasswordGenerator passwordGenerator, IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider, ILocalSam sam, IAppDataProvider appDataProvider, IMsMcsAdmPwdProvider msMcsAdmPwdProvider)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
            this.sam = sam;
            this.appDataProvider = appDataProvider;
            this.msMcsAdmPwdProvider = msMcsAdmPwdProvider;
        }

        public void DoCheck()
        {
            try
            {
                if (!this.settings.Enabled)
                {
                    this.logger.LogTrace(EventIDs.LapsAgentDisabled, "The LAPS agent is disabled");
                    return;
                }

                if (!this.settings.WriteToAppData && !this.settings.WriteToMsMcsAdmPasswordAttributes)
                {
                    this.logger.LogTrace(EventIDs.LapsAgentNotConfigured, "The LAPS agent is not configured to write passwords to any attribute stores");
                    return;
                }

                if (this.IsMsLapsInstalled())
                {
                    logger.LogWarning(EventIDs.LapsConflict, "The Microsoft LAPS client is installed and enabled. Disable the Microsoft LAPS agent via group policy or uninstall it to allow this tool to manage the local administrator password");
                    return;
                }

                IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

                var appData = this.appDataProvider.GetAppData(computer);

                if (this.HasPasswordExpired(appData, computer))
                {
                    logger.LogTrace(EventIDs.PasswordExpired, "Password has expired and needs to be changed");
                    this.ChangePassword(appData, computer);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }

        internal bool IsMsLapsInstalled()
        {
            RegistryKey r = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\77F1646A33805F848A7A683CFB6B88A7", false);

            if (r == null)
            {
                return false;
            }

            r = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft Services\AdmPwd", false);

            return r?.GetValue<int>("AdmPwdEnabled", 0) == 1;
        }

        internal bool HasPasswordExpired(IAppData appData, IComputer computer)
        {
            try
            {
                if (this.settings.WriteToAppData)
                {
                    if (appData.PasswordExpiry == null)
                    {
                        return false;
                    }

                    return DateTime.UtcNow > appData.PasswordExpiry;
                }
                else if (this.settings.WriteToMsMcsAdmPasswordAttributes)
                {
                    var expiry = this.msMcsAdmPwdProvider.GetExpiry(computer);

                    if (expiry == null)
                    {
                        return false;
                    }

                    return DateTime.UtcNow > expiry;
                }

                return false;
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.PasswordExpiryCheckFailure, ex, "Could not check the password expiry date");
                return false;
            }
        }

        internal void ChangePassword(IAppData appData, IComputer computer, SecurityIdentifier sid = null)
        {
            try
            {
                if (sid == null)
                {
                    sid = this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);
                }

                string newPassword = this.passwordGenerator.Generate();
                DateTime rotationInstant = DateTime.UtcNow;
                DateTime expiryDate = DateTime.UtcNow.AddDays(this.settings.MaximumPasswordAge);

                if (this.settings.WriteToAppData)
                {
                    appData.UpdateCurrentPassword(
                        this.encryptionProvider.Encrypt(
                            this.certificateProvider.FindCertificate(
                                false, this.settings.CertThumbprint, this.settings.CertPath),
                            newPassword),
                        rotationInstant,
                        expiryDate,
                        this.settings.PasswordHistoryDaysToKeep);
                    this.logger.LogTrace(EventIDs.SetPasswordOnAmAttribute, "Set password on Lithnet Access Manager attribute");
                }

                if (this.settings.WriteToMsMcsAdmPasswordAttributes)
                {
                    this.msMcsAdmPwdProvider.SetPassword(computer, newPassword, expiryDate);
                    this.logger.LogTrace(EventIDs.SetPasswordOnLapsAttribute, "Set password on Microsoft LAPS attribute");
                }
                else
                {
                    this.msMcsAdmPwdProvider.ClearPassword(computer);
                }

                this.sam.SetLocalAccountPassword(sid, newPassword);
                this.logger.LogInformation(EventIDs.SetPassword, "The local administrator password has been changed and will expire on {expiryDate}", expiryDate);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.PasswordChangeFailure, ex, "The password change operation failed");
            }
        }
    }
}
