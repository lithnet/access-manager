using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainPermissionViewModelFactory : IViewModelFactory<ActiveDirectoryDomainPermissionViewModel, Domain>
    {
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly ILogger<ActiveDirectoryDomainPermissionViewModel> logger;

        public ActiveDirectoryDomainPermissionViewModelFactory(IWindowsServiceProvider windowsServiceProvider, ILogger<ActiveDirectoryDomainPermissionViewModel> logger)
        {
            this.windowsServiceProvider = windowsServiceProvider;
            this.logger = logger;
        }

        public ActiveDirectoryDomainPermissionViewModel CreateViewModel(Domain model)
        {
            return new ActiveDirectoryDomainPermissionViewModel(model, windowsServiceProvider, logger);
        }
    }
}
