using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainPermissionViewModelFactory : IActiveDirectoryDomainPermissionViewModelFactory
    {
        private readonly IServiceSettingsProvider serviceSettings;
        private readonly ILogger<ActiveDirectoryDomainPermissionViewModel> logger;

        public ActiveDirectoryDomainPermissionViewModelFactory(IServiceSettingsProvider serviceSettings, ILogger<ActiveDirectoryDomainPermissionViewModel> logger)
        {
            this.serviceSettings = serviceSettings;
            this.logger = logger;
        }

        public ActiveDirectoryDomainPermissionViewModel CreateViewModel(Domain model)
        {
            return new ActiveDirectoryDomainPermissionViewModel(model, serviceSettings, logger);
        }
    }
}
