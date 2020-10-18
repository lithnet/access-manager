using System;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionViewModelFactory : NotificationChannelDefinitionViewModelFactory<PowershellNotificationChannelDefinition, PowershellNotificationChannelDefinitionViewModel>
    {
        private readonly IAppPathProvider appPathProvider;
        private readonly IFileSelectionViewModelFactory fileSelectionViewModelFactory;
        private readonly Func<IModelValidator<PowershellNotificationChannelDefinitionViewModel>> validator;
        private readonly ScriptTemplateProvider scriptTemplateProvider;

        public PowershellNotificationChannelDefinitionViewModelFactory(IAppPathProvider appPathProvider, IFileSelectionViewModelFactory fileSelectionViewModelFactory, Func<IModelValidator<PowershellNotificationChannelDefinitionViewModel>> validator, ScriptTemplateProvider scriptTemplateProvider)
        {
            this.appPathProvider = appPathProvider;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.validator = validator;
            this.scriptTemplateProvider = scriptTemplateProvider;
        }

        public override PowershellNotificationChannelDefinition CreateModel()
        {
            return new PowershellNotificationChannelDefinition();
        }

        public override PowershellNotificationChannelDefinitionViewModel CreateViewModel(PowershellNotificationChannelDefinition model)
        {
            return new PowershellNotificationChannelDefinitionViewModel(model, validator.Invoke(), fileSelectionViewModelFactory, appPathProvider, scriptTemplateProvider);
        }
    }
}
