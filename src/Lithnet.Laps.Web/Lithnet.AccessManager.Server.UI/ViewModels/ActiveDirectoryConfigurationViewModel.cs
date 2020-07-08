using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : PropertyChangedBase , IHaveDisplayName
    {
        public ActiveDirectoryConfigurationViewModel(IServiceSettingsProvider serviceSettings, IDirectory directory, IDialogCoordinator dialogCoordinator, ICertificateProvider certificateProvider)
        {
            this.Forests = new List<ActiveDirectoryForestConfigurationViewModel>();

            var domain = Domain.GetCurrentDomain();
            this.Forests.Add(new ActiveDirectoryForestConfigurationViewModel(domain.Forest, serviceSettings, directory, dialogCoordinator, certificateProvider));
            
            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                    var vm = new ActiveDirectoryForestConfigurationViewModel(forest, serviceSettings, directory, dialogCoordinator,  certificateProvider);
                    this.Forests.Add(vm);
                }
            }

            this.SelectedForest = this.Forests.FirstOrDefault();
        }

        public List<ActiveDirectoryForestConfigurationViewModel> Forests { get; }

        public ActiveDirectoryForestConfigurationViewModel SelectedForest { get; set; }

        public X509Certificate2 EncryptionCertificate { get; set; }

        public string EncryptionCertificateStatus { get; set; }

        public string DisplayName { get; set; } = "Active Directory";
    }
}
