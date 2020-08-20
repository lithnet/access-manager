using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthorizationViewModel : Screen, IHelpLink
    {
        private readonly AuthorizationOptions model;
        private readonly SecurityDescriptorTargetsViewModelFactory factory;
        private readonly IShellExecuteProvider shellExecuteProvider;


        public AuthorizationViewModel(AuthorizationOptions model, SecurityDescriptorTargetsViewModelFactory factory, IShellExecuteProvider shellExecuteProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.factory = factory;
            this.DisplayName = "Authorization";
        }

        protected override void OnInitialActivate()
        {
            Task.Run(() =>
            {
                this.Targets = this.factory.CreateViewModel(model.ComputerTargets);
            });
        }

        public SecurityDescriptorTargetsViewModel Targets { get; set; }

        public PackIconModernKind Icon => PackIconModernKind.Lock;

        public string HelpLink => Constants.HelpLinkPageAuthorization;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
