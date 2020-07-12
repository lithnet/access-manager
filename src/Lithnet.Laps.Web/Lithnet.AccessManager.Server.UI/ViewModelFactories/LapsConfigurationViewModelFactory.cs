using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class LapsConfigurationViewModelFactory : ILapsConfigurationViewModelFactory
    { 
        private readonly ICertificateProvider certificateProvider;

        private readonly IDirectory directory;

        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        public LapsConfigurationViewModelFactory(ICertificateProvider certificateProvider, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator)
        {
            this.certificateProvider = certificateProvider;
            this.directory = directory;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
        }

        public LapsConfigurationViewModel CreateViewModel()
        {
            return new LapsConfigurationViewModel(dialogCoordinator, certificateProvider, directory, certificate2ViewModelFactory);
        }
    }
}
