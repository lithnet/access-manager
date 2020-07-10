using System;
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

        private readonly IDirectory directory;

        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;

        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        private List<ActiveDirectoryDomainConfigurationViewModel> domains;

        private BindableCollection<X509Certificate2ViewModel> availableCertificates;

        public ActiveDirectoryForestConfigurationViewModel(Forest forest, IDialogCoordinator dialogCoordinator, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory, ICertificateProvider certificateProvider, IDirectory directory, IX509Certificate2ViewModelFactory certificate2ViewModelFactory)
        {
            this.forest = forest;
            this.directory = directory;
            this.certificateProvider = certificateProvider;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.domainFactory = domainFactory;
            this.dialogCoordinator = dialogCoordinator;

            this.PopulateLithnetSchemaStatus();
            this.PopulateMsLapsSchemaStatus();
            this.PopulatePamSupportState();
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }

        public List<ActiveDirectoryDomainConfigurationViewModel> Domains
        {
            get
            {
                if (this.domains == null)
                {
                    this.domains = new List<ActiveDirectoryDomainConfigurationViewModel>();

                    foreach (var domain in forest.Domains.OfType<Domain>())
                    {
                        this.Domains.Add(domainFactory.CreateViewModel(domain));
                    }

                    this.SelectedDomain = this.Domains.FirstOrDefault();
                }

                return this.domains;
            }
        }

        public ActiveDirectoryDomainConfigurationViewModel SelectedDomain { get; set; }

        public string DisplayName => this.forest.Name;

        public X509Certificate2ViewModel SelectedCertificate { get; set; }

        public BindableCollection<X509Certificate2ViewModel> AvailableCertificates
        {
            get
            {
                if (this.availableCertificates == null)
                {
                    this.BuildAvailableCertificates();
                }

                return this.availableCertificates;
            }
        }

        public string ForestFunctionalLevel
        {
            get
            {
                return this.forest.ForestModeLevel switch
                {
                    0 => "Windows 2000 Server",
                    1 => "Windows Server 2003 Mixed Mode",
                    2 => "Windows Server 2003",
                    3 => "Windows Server 2008",
                    4 => "Windows Server 2008 R2",
                    5 => "Windows Server 2012",
                    6 => "Windows Server 2012 R2",
                    var e when e >= 7 => "Windows Server 2016",
                    _ => "Unknown forest functional level"
                };
            }
        }

        public string MsLapsSchemaPresentText { get; set; }

        public string LithnetAccessManagerSchemaPresentText { get; set; }

        public string PamSupportState { get; set; }

        public bool CanPublishSelectedCertificate => !this.SelectedCertificate?.IsPublished ?? false;

        public void PublishSelectedCertificate()
        {
            var de = this.directory.GetConfigurationNamingContext(this.forest.RootDomain.Name);
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
                DataContext = vm, SaveButtonVisible = false, CancelButtonName = "Close"
            };

            w.ShowDialog();
        }

        public bool CanGenerateEncryptionCertificate { get; set; } = true;

        public void GenerateEncryptionCertificate()
        {
            X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(this.forest.Name);

            using X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
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

        public bool CanExtendSchemaMsLaps { get; set; }

        public void ExtendSchemaMsLaps()
        {

        }

        public bool CanExtendSchemaLithnetAccessManager { get; set; }

        public void ExtendSchemaLithnetAccessManager()
        {

        }

        private void BuildAvailableCertificates()
        {
            this.availableCertificates = new BindableCollection<X509Certificate2ViewModel>();

            var allCertificates = certificateProvider.GetEligibleCertificates(true).OfType<X509Certificate2>();
            this.certificateProvider.TryGetCertificateFromDirectory(out X509Certificate2 publishedCert, this.forest.RootDomain.Name);

            bool foundPublished = false;

            foreach (var certificate in allCertificates)
            {
                var vm = this.certificate2ViewModelFactory.CreateViewModel(certificate);

                if (certificate.Thumbprint == publishedCert?.Thumbprint)
                {
                    vm.IsPublished = true;
                    foundPublished = true;
                }

                this.availableCertificates.Add(vm);
            }

            if (!foundPublished)
            {
                var vm = this.certificate2ViewModelFactory.CreateViewModel(publishedCert);
                vm.IsOrphaned = true;
                vm.IsPublished = true;
                this.availableCertificates.Add(vm);
            }

            this.SelectedCertificate = this.AvailableCertificates.FirstOrDefault();
        }

        private void PopulatePamSupportState()
        {
            if (this.forest.ForestModeLevel < 2)
            {
                this.PamSupportState = "Just-in-time access is not supported in this forest";
                return;
            }

            if (!this.Is2016FunctionalLevel())
            {
                this.PamSupportState = "Just-in-time access is supported in this forest using legacy dynamic objects. Consider raising the forest functional level to Windows Server 2016 to enable time-based group membership support";
                return;
            }

            if (this.directory.IsPamFeatureEnabled(this.forest.RootDomain.Name))
            {
                this.PamSupportState = "Full support for just-in-time access using temporary group membership is available in this forest";
            }
            else
            {
                this.PamSupportState = "Just-in-time access is supported in this forest using legacy dynamic objects. Consider enabling the 'Privileged Access Management' feature in this forest to allow time-based group membership";
            }
        }

        private bool Is2016FunctionalLevel()
        {
            return this.forest.ForestModeLevel >= 7;
        }

        private void PopulateLithnetSchemaStatus()
        {
            try
            {
                var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.forest.Name));
                schema.FindProperty("lithnetEncryptedAdminPassword");
                this.CanExtendSchemaLithnetAccessManager = false;
                this.LithnetAccessManagerSchemaPresentText = "Present";
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                this.CanExtendSchemaLithnetAccessManager = true;
                this.LithnetAccessManagerSchemaPresentText = "Not present";
            }
            catch
            {
                this.CanExtendSchemaLithnetAccessManager = false;
                this.LithnetAccessManagerSchemaPresentText = "Error looking up schema";
            }
        }

        private void PopulateMsLapsSchemaStatus()
        {
            try
            {
                var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.forest.Name));
                schema.FindProperty("ms-Mcs-AdmPwd");
                this.CanExtendSchemaMsLaps = false;
                this.MsLapsSchemaPresentText = "Present";
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                this.CanExtendSchemaMsLaps = true;
                this.MsLapsSchemaPresentText = "Not present";
            }
            catch
            {
                this.CanExtendSchemaMsLaps = false;
                this.MsLapsSchemaPresentText = "Error looking up schema";
            }
        }
    }
}
