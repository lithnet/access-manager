using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestSchemaViewModelFactory : IActiveDirectoryForestSchemaViewModelFactory
    {
        private readonly ILogger<ActiveDirectoryForestSchemaViewModel> logger;

        public ActiveDirectoryForestSchemaViewModelFactory(ILogger<ActiveDirectoryForestSchemaViewModel> logger)
        {
            this.logger = logger;
        }

        public ActiveDirectoryForestSchemaViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestSchemaViewModel(model, logger);
        }
    }
}
