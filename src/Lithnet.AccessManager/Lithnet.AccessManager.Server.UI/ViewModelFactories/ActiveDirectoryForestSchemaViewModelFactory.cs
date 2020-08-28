using System.DirectoryServices.ActiveDirectory;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestSchemaViewModelFactory : IActiveDirectoryForestSchemaViewModelFactory
    {
        private readonly ILogger<ActiveDirectoryForestSchemaViewModel> logger;
        private readonly IDirectory directory;

        public ActiveDirectoryForestSchemaViewModelFactory(ILogger<ActiveDirectoryForestSchemaViewModel> logger, IDirectory directory)
        {
            this.logger = logger;
            this.directory = directory;
        }

        public ActiveDirectoryForestSchemaViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestSchemaViewModel(model, logger, directory);
        }
    }
}
