using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectorySchemaViewModelFactory : IActiveDirectorySchemaViewModelFactory
    {
        private readonly IActiveDirectoryForestConfigurationViewModelFactory forestFactory;

        public ActiveDirectorySchemaViewModelFactory(IActiveDirectoryForestConfigurationViewModelFactory forestFactory)
        {
            this.forestFactory = forestFactory;
        }

        public ActiveDirectorySchemaViewModel CreateViewModel()
        {
            return new ActiveDirectorySchemaViewModel(this.forestFactory);
        }
    }
}
