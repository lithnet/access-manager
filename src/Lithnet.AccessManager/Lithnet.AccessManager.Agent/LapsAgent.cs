﻿using System;
using System.Security.Principal;
using System.ServiceModel.Configuration;
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

        private readonly ILocalSam sam;

        private readonly ILithnetAdminPasswordProvider lithnetAdminPasswordProvider;

        private bool mslapsInstaled;

        private bool isDisabledLogged;

        public LapsAgent(ILogger<LapsAgent> logger, IDirectory directory, ILapsSettings settings, IPasswordGenerator passwordGenerator, ILocalSam sam, ILithnetAdminPasswordProvider lithnetAdminPasswordProvider)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.sam = sam;
            this.lithnetAdminPasswordProvider = lithnetAdminPasswordProvider;
        }

        public void DoCheck()
        {
            try
            {
                if (!this.settings.Enabled)
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

                if (this.IsMsLapsInstalled())
                {
                    if (!this.mslapsInstaled)
                    {
                        logger.LogWarning(EventIDs.LapsConflict, "The Microsoft LAPS client is installed and enabled. Disable the Microsoft LAPS agent via group policy or uninstall it to allow this tool to manage the local administrator password");
                        mslapsInstaled = true;
                    }

                    return;
                }
                else
                {
                    if (mslapsInstaled)
                    {
                        mslapsInstaled = false;
                        logger.LogInformation(EventIDs.LapsConflictResolved, "The Microsoft LAPS client has been removed or disabled. Lithnet Access Manager will now set the local admin password for this machine");
                    }
                }

                IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

                if (this.HasPasswordExpired(computer))
                {
                    logger.LogTrace(EventIDs.PasswordExpired, "Password has expired and needs to be changed");
                    this.ChangePassword(computer);
                }
                //else
                //{
                //    logger.LogTrace(EventIDs.PasswordChangeNotRequired, "Password does not need to be changed");
                //}
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }

        public bool IsMsLapsInstalled()
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

        public bool HasPasswordExpired(IComputer computer)
        {
            try
            {
                return this.lithnetAdminPasswordProvider.HasPasswordExpired(computer, this.settings.MsMcsAdmPwdBehaviour == MsMcsAdmPwdBehaviour.Populate);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.PasswordExpiryCheckFailure, ex, "Could not check the password expiry date");
                return false;
            }
        }

        public void ChangePassword(IComputer computer, SecurityIdentifier sid = null)
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

                lithnetAdminPasswordProvider.UpdateCurrentPassword(computer, newPassword, rotationInstant, expiryDate, this.settings.PasswordHistoryDaysToKeep, this.settings.MsMcsAdmPwdBehaviour);

                this.logger.LogTrace(EventIDs.SetPasswordOnAmAttribute, "Set password on Lithnet Access Manager attribute");
                
                if (this.settings.MsMcsAdmPwdBehaviour == MsMcsAdmPwdBehaviour.Populate)
                {
                    this.logger.LogTrace(EventIDs.SetPasswordOnLapsAttribute, "Set password on Microsoft LAPS attribute");
                }
               
                this.sam.SetLocalAccountPassword(sid, newPassword);
                this.logger.LogInformation(EventIDs.SetPassword, "The local administrator password has been changed and will expire on {expiryDate}", expiryDate.ToLocalTime());
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.PasswordChangeFailure, ex, "The password change operation failed");
            }
        }
    }
}
