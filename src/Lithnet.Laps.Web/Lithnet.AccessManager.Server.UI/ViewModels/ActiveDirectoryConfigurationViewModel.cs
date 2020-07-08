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
        public ActiveDirectoryConfigurationViewModel(IActiveDirectoryForestConfigurationViewModelFactory forestFactory)
        {
            this.Forests = new List<ActiveDirectoryForestConfigurationViewModel>();

            var domain = Domain.GetCurrentDomain();
            this.Forests.Add(forestFactory.CreateViewModel(domain.Forest));
            
            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                    var vm = forestFactory.CreateViewModel(forest);
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
