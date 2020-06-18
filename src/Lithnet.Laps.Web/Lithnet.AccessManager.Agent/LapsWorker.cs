using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Time;

namespace Lithnet.AccessManager.Agent
{
    public class LapsWorker
    {
        private readonly ILogger<LapsWorker> logger;

        private readonly IDirectory directory;

        private readonly ILapsSettingsProvider settings;

        private readonly IPasswordGenerator passwordGenerator;

        private readonly IEncryptionProvider encryptionProvider;

        private readonly ICertificateResolver certificateResolver;

        public LapsWorker(ILogger<LapsWorker> logger, IDirectory directory, ILapsSettingsProvider settings, IPasswordGenerator passwordGenerator, IEncryptionProvider encryptionProvider, ICertificateResolver certificateResolver)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.encryptionProvider = encryptionProvider;
            this.certificateResolver = certificateResolver;
        }

        public void DoCheck()
        {
            if (!this.settings.LapsEnabled)
            {
                return;
            }

            IComputer computer = this.directory.GetComputer();

            var lam = this.directory.GetLamSettings(computer);

            if (lam.PasswordExpiry > DateTime.UtcNow)
            {
                this.ChangePassword(lam, computer);
            }
        }

        private void ChangePassword(ILamSettings lam, IComputer computer)
        {
            SecurityIdentifier localAdminSid = this.directory.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);

            string newPassword = this.passwordGenerator.Generate();
            DateTime rotationInstant = DateTime.UtcNow;
            DateTime expiryDate = DateTime.UtcNow.AddDays(this.settings.MaximumPasswordAge);

            ProtectedPasswordHistoryItem pphi = new ProtectedPasswordHistoryItem()
            {
                Created = rotationInstant,
                EncryptedData = this.encryptionProvider.Encrypt(this.certificateResolver.GetEncryptionCertificate(), newPassword),
            };

            List<ProtectedPasswordHistoryItem> items = this.PruneHistory(lam.PasswordHistory, rotationInstant);
            items.Add(pphi);

            lam.ReplacePasswordHistory(items);

            using (var context = new PrincipalContext(ContextType.Machine))
            {
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, localAdminSid.ToString()))
                {
                    user.SetPassword(newPassword);
                }
            }

            if (this.settings.WriteToMsMcsAdmPasswordAttributes)
            {
                this.directory.UpdateMsMcsAdmPwdAttribute(computer, newPassword, expiryDate);
            }
        }

        private List<ProtectedPasswordHistoryItem> PruneHistory(IReadOnlyCollection<ProtectedPasswordHistoryItem> items, DateTime rotationInstant)
        {
            List<ProtectedPasswordHistoryItem> newItems = new List<ProtectedPasswordHistoryItem>();

            foreach (var item in items)
            {
                if (item.Retired == null)
                {
                    item.Retired = rotationInstant;
                }

                if (item.Retired.Value.AddDays(this.settings.PasswordHistoryDaysToKeep) < DateTime.UtcNow)
                {
                    newItems.Add(item);
                }
            }

            return newItems;
        }
    }
}
