using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class HighAvailabilityViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILicenseManager licenseManager;
        private readonly ILogger<HighAvailabilityViewModel> logger;

        public HighAvailabilityViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILicenseManager licenseManager, ILogger<HighAvailabilityViewModel> logger, INotifyModelChangedEventPublisher eventPublisher)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.DisplayName = "High availability";
            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconMaterialKind Icon => PackIconMaterialKind.Server;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public void LinkHaLearnMore()
        {
        }

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public string ClusterEncryptionKeyDisplayName { get; set; }

        public bool IsClusterEncryptionKeyPresent { get; set; }

        public bool IsClusterEncryptionKeyMissing => !this.IsClusterEncryptionKeyPresent;

        public bool UseLocalDB { get; set; }

        public bool UseSqlServer { get; set; }

        public string ConnectionString { get; set; }

        public void ClusterEncryptionKeyGenerate()
        {

        }

        public void ClusterEncryptionKeyExport()
        {

        }

        public void ClusterEncryptionKeyImport()
        {

        }
    }
}
