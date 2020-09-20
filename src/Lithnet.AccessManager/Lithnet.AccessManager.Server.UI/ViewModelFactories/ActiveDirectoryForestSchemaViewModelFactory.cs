using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestSchemaViewModelFactory : IActiveDirectoryForestSchemaViewModelFactory
    {
        private readonly ILogger<ActiveDirectoryForestSchemaViewModel> logger;
        private readonly IDiscoveryServices discoveryServices;

        public ActiveDirectoryForestSchemaViewModelFactory(ILogger<ActiveDirectoryForestSchemaViewModel> logger, IDiscoveryServices discoveryServices)
        {
            this.logger = logger;
            this.discoveryServices = discoveryServices;
        }

        public ActiveDirectoryForestSchemaViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestSchemaViewModel(model, logger, discoveryServices);
        }
    }
}
