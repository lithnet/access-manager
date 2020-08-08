using System.Security.Cryptography.X509Certificates;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModelFactory : IX509Certificate2ViewModelFactory
    {
        private readonly IServiceSettingsProvider serviceProvider;
        private readonly ILogger<X509Certificate2ViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;

        public X509Certificate2ViewModelFactory(IServiceSettingsProvider serviceProvider, ILogger<X509Certificate2ViewModel> logger, IDialogCoordinator dialogCoordinator)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
        }

        public X509Certificate2ViewModel CreateViewModel(X509Certificate2 model)
        {
            return new X509Certificate2ViewModel(model, serviceProvider, logger, dialogCoordinator);
        }
    }
}
