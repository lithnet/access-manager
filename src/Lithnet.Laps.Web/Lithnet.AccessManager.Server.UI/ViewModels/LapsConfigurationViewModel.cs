using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class LapsConfigurationViewModel : PropertyChangedBase, IViewAware, IHaveDisplayName
    {
        private readonly ICertificateProvider certificateProvider;

        private readonly IDirectory directory;

        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IServiceSettingsProvider serviceSettings;

        public LapsConfigurationViewModel(IDialogCoordinator dialogCoordinator, ICertificateProvider certificateProvider, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IServiceSettingsProvider serviceSettings)
        {
            this.directory = directory;
            this.certificateProvider = certificateProvider;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettings = serviceSettings;

            this.Forests = new List<Forest>();

            var domain = Domain.GetCurrentDomain();
            this.Forests.Add(domain.Forest);

            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                    this.Forests.Add(forest);
                }
            }

            this.SelectedForest = this.Forests.FirstOrDefault();
            this.AvailableCertificates = new BindableCollection<X509Certificate2ViewModel>();
            this.RefreshAvailableCertificates();
        }

        public List<Forest> Forests { get; }

        public Forest SelectedForest { get; set; }

        private void OnSelectedForestChanged()
        {
            this.RefreshAvailableCertificates();
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }

        public X509Certificate2ViewModel SelectedCertificate { get; set; }

        [PropertyChanged.DependsOn(nameof(SelectedForest))]
        public BindableCollection<X509Certificate2ViewModel> AvailableCertificates { get; }

        public bool CanPublishSelectedCertificate => !this.SelectedCertificate?.IsPublished ?? false;

        public void PublishSelectedCertificate()
        {
            var de = this.directory.GetConfigurationNamingContext(this.SelectedForest.RootDomain.Name);
            var certData = Convert.ToBase64String(this.SelectedCertificate.Model.RawData, Base64FormattingOptions.InsertLineBreaks);

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script to publish the encryption certificate",
                ScriptText = ScriptTemplates.PublishCertificateTemplate
                    .Replace("{configurationNamingContext}", de.GetPropertyString("distinguishedName"))
                    .Replace("{certificateData}", certData)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            if (this.certificateProvider.TryGetCertificateFromDirectory(out X509Certificate2 publishedCert,
                this.SelectedForest.RootDomain.Name))
            {
                if (publishedCert.Thumbprint == this.SelectedCertificate.Model.Thumbprint)
                {
                    this.SelectedCertificate.IsPublished = true;

                    foreach (var c in this.AvailableCertificates.ToList())
                    {
                        if (this.SelectedCertificate != c)
                        {
                            c.IsPublished = false;
                        }

                        if (c.IsOrphaned)
                        {
                            this.AvailableCertificates.Remove(c);
                        }
                    }
                }
            }
        }

        public bool CanGenerateEncryptionCertificate { get; set; } = true;

        public void GenerateEncryptionCertificate()
        {
            X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(this.SelectedForest.Name);

            using X509Store store =  this.certificateProvider.OpenServiceStore(Constants.ServiceName, OpenFlags.ReadWrite);
            store.Add(cert);

            var vm = this.certificate2ViewModelFactory.CreateViewModel(cert);

            this.AvailableCertificates.Add(vm);
            this.SelectedCertificate = vm;
        }

        public bool CanShowCertificateDialog => this.SelectedCertificate != null;

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.SelectedCertificate.Model, this.GetHandle());
        }

        public void DelegateServicePermission()
        {
            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read the encrypted local admin passwords and history from the directory",
                ScriptText = ScriptTemplates.GrantAccessManagerPermissions.Replace("{serviceAccount}", this.serviceSettings.GetServiceAccount().ToString(), StringComparison.OrdinalIgnoreCase)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }

        private void RefreshAvailableCertificates()
        {
            if (this.AvailableCertificates == null)
            {
                return;
            }

            this.AvailableCertificates.Clear();

            if (this.SelectedForest == null)
            {
                return;
            }

            var allCertificates = certificateProvider.GetEligibleCertificates(false).OfType<X509Certificate2>();
            this.certificateProvider.TryGetCertificateFromDirectory(out X509Certificate2 publishedCert, this.SelectedForest.RootDomain.Name);

            bool foundPublished = false;

            foreach (var certificate in allCertificates)
            {
                var vm = this.certificate2ViewModelFactory.CreateViewModel(certificate);

                if (certificate.Thumbprint == publishedCert?.Thumbprint)
                {
                    vm.IsPublished = true;
                    foundPublished = true;
                }

                if (certificate.Subject.StartsWith($"CN={this.SelectedForest.RootDomain.Name}", StringComparison.OrdinalIgnoreCase))
                {
                    this.AvailableCertificates.Add(vm);
                }
            }

            if (!foundPublished && publishedCert != null)
            {
                var vm = this.certificate2ViewModelFactory.CreateViewModel(publishedCert);
                vm.IsOrphaned = true;
                vm.IsPublished = true;
                this.AvailableCertificates.Add(vm);
            }
        }

        public string DisplayName { get; set; } = "Password encryption and history";
    }
}
