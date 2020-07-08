using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModel : PropertyChangedBase, IViewAware
    {
        private readonly Forest forest;


        private readonly ICertificateProvider certificateProvider;

        public ActiveDirectoryForestConfigurationViewModel(Forest forest, IServiceSettingsProvider serviceSettings, IDirectory directory, IDialogCoordinator dialogCoordinator, ICertificateProvider certificateProvider)
        {
            this.forest = forest;
            this.certificateProvider = certificateProvider;

            this.Domains = new List<ActiveDirectoryDomainConfigurationViewModel>();

            foreach (var domain in forest.Domains.OfType<Domain>())
            {
                this.Domains.Add(new ActiveDirectoryDomainConfigurationViewModel(domain, serviceSettings, directory, dialogCoordinator));
            }

            this.SelectedDomain = this.Domains.FirstOrDefault();

            this.AvailableCertificates = new BindableCollection<X509Certificate2>();
            this.AvailableCertificates.AddRange(certificateProvider.GetEligibleCertificates().OfType<X509Certificate2>());
        }

        public List<ActiveDirectoryDomainConfigurationViewModel> Domains { get; }

        public ActiveDirectoryDomainConfigurationViewModel SelectedDomain { get; set; }

        public string DisplayName => this.forest.Name;

        public X509Certificate2 SelectedCertificate { get; set; }

        public string SelectedCertificateDisplayName => this.SelectedCertificate?.ToDisplayName();

        public BindableCollection<X509Certificate2> AvailableCertificates { get; }

        public string EncryptionCertificateStatus { get; set; }

        public bool IsEncryptionCertificatePublished { get; set; }

        public bool IsMsLapsSchemaPresent { get; set; }

        public bool IsLithnetAccessManagerSchemaPresent { get; set; }



        public bool CanPublishEncryptionCertificate { get; set; }

        public void PublishEncryptionCertificate()
        {
        }

        public bool CanGenerateEncryptionCertificate { get; set; } = true;

        public void GenerateEncryptionCertificate()
        {
            X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(this.forest.Name);
            using X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
            this.AvailableCertificates.Add(cert);
            this.SelectedCertificate = cert;
        }

        public bool CanShowCertificateDialog => this.SelectedCertificate != null;

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.SelectedCertificate, this.GetHandle());
        }

        public void ShowCertificateSelectDialog()
        {
            X509Certificate2Collection results = X509Certificate2UI.SelectFromCollection(this.certificateProvider.GetEligibleCertificates(), "Select encryption certificate", "Select a certificate to use", X509SelectionFlag.SingleSelection, this.GetHandle());

            if (results.Count == 1)
            {
                this.SelectedCertificate = results[0];
            }
        }

        public bool CanDelegateJitPermission { get; set; }

        public void DelegateJitPermission()
        {
        }

        public bool CanDelegateMsLapsPermission { get; set; }

        public void DelegateMsLapsPermission()
        {
        }

        public bool CanDelegateLithnetLapsPermission { get; set; }

        public void DelegateLithnetAccessManagerPermission()
        {
        }

        public bool CanExtendSchemaMsLaps { get; set; }

        public void ExtendSchemaMsLaps()
        {
        }

        public bool CanExtendSchemaLithnetAccessManager { get; set; }

        public void ExtendSchemaLithnetAccessManager()
        {
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }
    }
}
