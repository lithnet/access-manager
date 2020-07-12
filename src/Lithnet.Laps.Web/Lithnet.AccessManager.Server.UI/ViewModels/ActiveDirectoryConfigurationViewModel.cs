using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        public ActiveDirectoryConfigurationViewModel(IActiveDirectorySchemaViewModelFactory schemaFactory, ILapsConfigurationViewModelFactory lapsFactory, IJitConfigurationViewModelFactory jitFactory)
        {
            this.DisplayName = "Active Directory";

            this.Items.Add(schemaFactory.CreateViewModel());
            this.Items.Add(lapsFactory.CreateViewModel());
            this.Items.Add(jitFactory.CreateViewModel());

            this.ActiveItem = this.Items.FirstOrDefault();
        }

        public sealed override void ActivateItem(PropertyChangedBase item)
        {
            base.ActivateItem(item);
        }
    }
}
