using System.Threading;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly IFileSelectionViewModelFactory fileSelectionViewModelFactory;
        private readonly IModelValidator<PowershellNotificationChannelDefinitionViewModel> validator;

        public PowershellNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, INotificationChannelSelectionViewModelFactory channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IModelValidator<PowershellNotificationChannelDefinitionViewModel> validator)
        {
            this.appPathProvider = appPathProvider;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.validator = validator;
        }

        public override PowershellNotificationChannelDefinition CreateModel()
        {
            return new PowershellNotificationChannelDefinition();
        }

        public override PowershellNotificationChannelDefinitionViewModel CreateViewModel(PowershellNotificationChannelDefinition model)
        {
            return new PowershellNotificationChannelDefinitionViewModel(model, validator, fileSelectionViewModelFactory, appPathProvider);
        }
    }
}
