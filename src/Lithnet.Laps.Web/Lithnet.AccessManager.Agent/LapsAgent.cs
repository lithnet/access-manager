using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class LapsAgent : ILapsAgent
    {
        private readonly ILogger<LapsAgent> logger;

        private readonly IDirectory directory;

        private readonly ILapsSettings settings;

        private readonly IPasswordGenerator passwordGenerator;

        private readonly IEncryptionProvider encryptionProvider;

        private readonly ICertificateResolver certificateResolver;

        private readonly ILocalSam sam;

        private readonly IAppDataProvider appDataProvider;

        private readonly IMsMcsAdmPwdProvider msMcsAdmPwdProvider;

        public LapsAgent(ILogger<LapsAgent> logger, IDirectory directory, ILapsSettings settings, IPasswordGenerator passwordGenerator, IEncryptionProvider encryptionProvider, ICertificateResolver certificateResolver, ILocalSam sam, IAppDataProvider appDataProvider, IMsMcsAdmPwdProvider msMcsAdmPwdProvider)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.encryptionProvider = encryptionProvider;
            this.certificateResolver = certificateResolver;
            this.sam = sam;
            this.appDataProvider = appDataProvider;
            this.msMcsAdmPwdProvider = msMcsAdmPwdProvider;
        }

        public void DoCheck()
        {
            if (!this.settings.Enabled)
            {
                return;
            }

            if (!this.settings.WriteToAppData && !this.settings.WriteToMsMcsAdmPasswordAttributes)
            {
                return;
            }

            IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

            var appData = this.appDataProvider.GetAppData(computer);

            if (this.HasPasswordExpired(appData, computer))
            {
                this.ChangePassword(appData, computer);
            }
        }

        internal bool HasPasswordExpired(IAppData appData, IComputer computer)
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

        internal void ChangePassword(IAppData appData, IComputer computer, SecurityIdentifier sid = null)
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
                        this.certificateResolver.GetEncryptionCertificate(
                            this.settings.CertThumbprint),
                        newPassword), 
                    rotationInstant, 
                    expiryDate, 
                    this.settings.PasswordHistoryDaysToKeep);
            }

            if (this.settings.WriteToMsMcsAdmPasswordAttributes)
            {
                this.msMcsAdmPwdProvider.SetPassword(computer, newPassword, expiryDate);
            }

            this.sam.SetLocalAccountPassword(sid, newPassword);
        }
    }
}
