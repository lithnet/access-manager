using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectorySchemaViewModel : PropertyChangedBase , IHaveDisplayName
    {
        public string DisplayName { get; set; } = "Active Directory";

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public ActiveDirectorySchemaViewModel(IActiveDirectoryForestConfigurationViewModelFactory forestFactory)
        {
            this.Forests = new List<ActiveDirectoryForestConfigurationViewModel>();

            var domain = Domain.GetCurrentDomain();
            this.Forests.Add(forestFactory.CreateViewModel(domain.Forest));
            
            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                    this.Forests.Add(forestFactory.CreateViewModel(forest));
                }
            }

            this.SelectedForest = this.Forests.FirstOrDefault();
        }

        public List<ActiveDirectoryForestConfigurationViewModel> Forests { get; }

        public ActiveDirectoryForestConfigurationViewModel SelectedForest { get; set; }
    }
}
