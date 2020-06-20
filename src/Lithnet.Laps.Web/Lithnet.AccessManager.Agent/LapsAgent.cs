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

        private readonly IAppDataProvider provider;

        public LapsAgent(ILogger<LapsAgent> logger, IDirectory directory, ILapsSettings settings, IPasswordGenerator passwordGenerator, IEncryptionProvider encryptionProvider, ICertificateResolver certificateResolver, ILocalSam sam, IAppDataProvider provider)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.encryptionProvider = encryptionProvider;
            this.certificateResolver = certificateResolver;
            this.sam = sam;
            this.provider = provider;
        }

        public void DoCheck()
        {
            if (!this.settings.LapsEnabled)
            {
                return;
            }

            if (!this.settings.WriteToAppData && !this.settings.WriteToMsMcsAdmPasswordAttributes)
            {
                return;
            }

            IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

            var appData = this.provider.GetAppData(computer);

            if (appData.PasswordExpiry > DateTime.UtcNow)
            {
                this.ChangePassword(appData, computer);
            }
        }

        private void ChangePassword(IAppData appData, IComputer computer)
        {
            SecurityIdentifier localAdminSid = this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            string newPassword = this.passwordGenerator.Generate();
            DateTime rotationInstant = DateTime.UtcNow;
            DateTime expiryDate = DateTime.UtcNow.AddDays(this.settings.MaximumPasswordAge);

            if (this.settings.WriteToAppData)
            {
                appData.UpdateCurrentPassword(this.encryptionProvider.Encrypt(this.certificateResolver.GetEncryptionCertificate(this.settings.SigningCertThumbprint), newPassword), rotationInstant, expiryDate, this.settings.PasswordHistoryDaysToKeep);
            }

            if (this.settings.WriteToMsMcsAdmPasswordAttributes)
            {
                this.directory.UpdateMsMcsAdmPwdAttribute(computer, newPassword, expiryDate);
            }

            using (var context = new PrincipalContext(ContextType.Machine))
            {
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, localAdminSid.ToString()))
                {
                    user.SetPassword(newPassword);
                }
            }
        }
    }
}
