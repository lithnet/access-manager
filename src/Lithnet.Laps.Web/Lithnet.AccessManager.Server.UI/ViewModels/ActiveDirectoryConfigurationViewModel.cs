using System.Linq;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        public ActiveDirectoryConfigurationViewModel(IActiveDirectorySchemaViewModelFactory schemaFactory, ILapsConfigurationViewModelFactory lapsFactory, IJitConfigurationViewModelFactory jitFactory)
        {
            this.DisplayName = "Active Directory";

            this.Items.Add(schemaFactory.CreateViewModel());

            this.ActiveItem = this.Items.FirstOrDefault();
        }

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;
    }
}
