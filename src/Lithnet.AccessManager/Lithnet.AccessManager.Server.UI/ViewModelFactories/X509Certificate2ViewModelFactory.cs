using System.Security.Cryptography.X509Certificates;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModelFactory : IX509Certificate2ViewModelFactory
    {
        private readonly ILogger<X509Certificate2ViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ICertificatePermissionProvider certPermissionProvider;

        public X509Certificate2ViewModelFactory(ILogger<X509Certificate2ViewModel> logger, IDialogCoordinator dialogCoordinator, ICertificatePermissionProvider certPermissionProvider)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.certPermissionProvider = certPermissionProvider;
        }

        public X509Certificate2ViewModel CreateViewModel(X509Certificate2 model)
        {
            return new X509Certificate2ViewModel(model, logger, dialogCoordinator, certPermissionProvider);
        }
    }
}
