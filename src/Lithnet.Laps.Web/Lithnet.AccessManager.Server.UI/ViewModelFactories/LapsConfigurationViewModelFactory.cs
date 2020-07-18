using System.Windows.Media.Animation;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public class LapsConfigurationViewModelFactory : ILapsConfigurationViewModelFactory
    { 
        private readonly ICertificateProvider certificateProvider;

        private readonly IDirectory directory;

        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IServiceSettingsProvider serviceSettings;

        public LapsConfigurationViewModelFactory(ICertificateProvider certificateProvider, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator, IServiceSettingsProvider serviceSettings)
        {
            this.certificateProvider = certificateProvider;
            this.directory = directory;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettings = serviceSettings;
        }

        public LapsConfigurationViewModel CreateViewModel()
        {
            return new LapsConfigurationViewModel(dialogCoordinator, certificateProvider, directory, certificate2ViewModelFactory, serviceSettings);
        }
    }
}
