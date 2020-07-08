using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModelFactory : IActiveDirectoryForestConfigurationViewModelFactory
    {
        private readonly ICertificateProvider certificateProvider;

        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        public ActiveDirectoryForestConfigurationViewModelFactory(ICertificateProvider certificateProvider, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory)
        {
            this.certificateProvider = certificateProvider;
            this.domainFactory = domainFactory;
        }

        public ActiveDirectoryForestConfigurationViewModel CreateViewModel(Forest model)
        {
            return new ActiveDirectoryForestConfigurationViewModel(model, domainFactory, certificateProvider);
        }
    }
}
