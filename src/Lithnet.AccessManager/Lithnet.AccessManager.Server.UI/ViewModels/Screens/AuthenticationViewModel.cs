using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthenticationViewModel : Screen, IHelpLink
    {
        private readonly AuthenticationOptions model;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> x509ViewModelFactory;
        private readonly ILogger logger;
        private readonly IActiveDirectory directory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IProtectedSecretProvider secretProvider;

        public AuthenticationViewModel(AuthenticationOptions model, ILogger<AuthenticationViewModel> logger, INotifyModelChangedEventPublisher eventPublisher, IDialogCoordinator dialogCoordinator, IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> x509ViewModelFactory, IActiveDirectory directory, IShellExecuteProvider shellExecuteProvider, IObjectSelectionProvider objectSelectionProvider, IProtectedSecretProvider secretProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.x509ViewModelFactory = x509ViewModelFactory;
            this.logger = logger;
            this.directory = directory;
            this.eventPublisher = eventPublisher;
            this.objectSelectionProvider = objectSelectionProvider;
            this.secretProvider = secretProvider;

            this.DisplayName = "User authentication";

            model.Iwa ??= new IwaAuthenticationProviderOptions();
            model.Oidc ??= new OidcAuthenticationProviderOptions();
            model.WsFed ??= new WsFedAuthenticationProviderOptions();
            model.ClientCert ??= new CertificateAuthenticationProviderOptions();
            model.ClientCert.TrustedIssuers ??= new List<string>();
            model.ClientCert.RequiredEkus ??= new List<string>();
            model.AllowedPrincipals ??= new List<string>();

            this.TrustedIssuers = new BindableCollection<X509Certificate2ViewModel>();
            this.RequiredEkus = new BindableCollection<string>(model.ClientCert.RequiredEkus);
            this.AllowedPrincipals = new BindableCollection<SecurityIdentifierViewModel>();
        }

        public string HelpLink => Constants.HelpLinkPageAuthentication;
        protected override void OnInitialActivate()
        {
            Task.Run(this.Initialize);
        }

        private void Initialize()
        {
            try
            {
                this.BuildAllowedToAuthenticateList();
                this.BuildTrustedIssuers();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
            finally
            {
                this.eventPublisher.Register(this);
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        private void BuildAllowedToAuthenticateList()
        {
            this.AllowedPrincipals.Clear();

            foreach (var item in this.model.AllowedPrincipals)
            {
                if (item.TryParseAsSid(out SecurityIdentifier sid))
                {
                    SecurityIdentifierViewModel vm = new SecurityIdentifierViewModel(sid, directory);
                    this.AllowedPrincipals.Add(vm);
                }
            }
        }

        public SecurityIdentifierViewModel SelectedAllowedPrincipal { get; set; }

        public bool CanRemoveAllowedPrincipal => this.SelectedAllowedPrincipal != null;

        public async Task RemoveAllowedPrincipal()
        {
            try
            {
                SecurityIdentifierViewModel selected = this.SelectedAllowedPrincipal;

                if (selected == null)
                {
                    return;
                }

                this.model.AllowedPrincipals.Remove(selected.Sid);
                this.AllowedPrincipals.Remove(selected);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task AddAllowedPrincipal()
        {
            try
            {
                if (this.objectSelectionProvider.GetUserOrGroup(this, out SecurityIdentifier sid))
                {
                    SecurityIdentifierViewModel sidvm = new SecurityIdentifierViewModel(sid, directory);

                    if (this.model.AllowedPrincipals.Any(t => string.Equals(t, sidvm.Sid, StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    this.model.AllowedPrincipals.Add(sidvm.Sid);
                    this.AllowedPrincipals.Add(sidvm);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        [NotifyModelChangedCollection(RequiresServiceRestart = true)]
        public BindableCollection<SecurityIdentifierViewModel> AllowedPrincipals { get; }

        private void BuildTrustedIssuers()
        {
            this.TrustedIssuers.Clear();

            int count = 0;

            foreach (string cert in this.model.ClientCert.TrustedIssuers)
            {
                count++;

                try
                {
                    byte[] bcert = Convert.FromBase64String(cert);
                    var x = new X509Certificate2(bcert);
                    this.TrustedIssuers.Add(this.x509ViewModelFactory.CreateViewModel(x));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.UIGenericError, ex, $"The trusted issuer certificate at position {count} could not be parsed");
                }
            }
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public AuthenticationMode AuthenticationMode { get => this.model.Mode; set => this.model.Mode = value; }

        public IEnumerable<AuthenticationMode> AuthenticationModeValues
        {
            get
            {
                return Enum.GetValues(typeof(AuthenticationMode)).Cast<AuthenticationMode>();
            }
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public AuthenticationSchemes IwaAuthenticationSchemes { get => this.model.Iwa.AuthenticationSchemes; set => this.model.Iwa.AuthenticationSchemes = value; }

        public IEnumerable<AuthenticationSchemes> IwaAuthenticationSchemesValues
        {
            get
            {
                return Enum.GetValues(typeof(AuthenticationSchemes)).Cast<AuthenticationSchemes>().Where(t => t > 0);
            }
        }

        public bool OidcVisible => this.AuthenticationMode == AuthenticationMode.Oidc;

        public bool WsFedVisible => this.AuthenticationMode == AuthenticationMode.WsFed;

        public bool IwaVisible => this.AuthenticationMode == AuthenticationMode.Iwa;

        public bool CertificateVisible => this.AuthenticationMode == AuthenticationMode.Certificate;

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public string OidcAuthority { get => this.model.Oidc.Authority; set => this.model.Oidc.Authority = value; }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public string OidcClientID { get => this.model.Oidc.ClientID; set => this.model.Oidc.ClientID = value; }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public string OidcSecret
        {
            get => this.model.Oidc.Secret?.Data == null ? null : "-placeholder-";
            set
            {
                if (value != "-placeholder-")
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        this.model.Oidc.Secret = null;
                        return;
                    }

                    this.model.Oidc.Secret = this.secretProvider.ProtectSecret(value);
                }
            }
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public string WsFedRealm { get => this.model.WsFed.Realm; set => this.model.WsFed.Realm = value; }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public string WsFedMetadata { get => this.model.WsFed.Metadata; set => this.model.WsFed.Metadata = value; }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool RequireSmartCardEku
        {
            get => this.model.ClientCert.RequireSmartCardLogonEku;
            set => this.model.ClientCert.RequireSmartCardLogonEku = value;
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        [DependsOn(nameof(ValidateAnyTrustedIssuer), nameof(ValidateSpecificIssuer))]
        public bool ValidateToNTAuth
        {
            get => this.model.ClientCert.ValidationMethod == ClientCertificateValidationMethod.NtAuthStore;
            set
            {
                if (value)
                {
                    this.model.ClientCert.ValidationMethod = ClientCertificateValidationMethod.NtAuthStore;
                }
            }
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        [DependsOn(nameof(ValidateToNTAuth), nameof(ValidateSpecificIssuer))]
        public bool ValidateAnyTrustedIssuer
        {
            get => this.model.ClientCert.ValidationMethod == ClientCertificateValidationMethod.AnyTrustedIssuer;
            set
            {
                if (value)
                {
                    this.model.ClientCert.ValidationMethod = ClientCertificateValidationMethod.AnyTrustedIssuer;
                }
            }
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        [DependsOn(nameof(ValidateToNTAuth), nameof(ValidateAnyTrustedIssuer))]
        public bool ValidateSpecificIssuer
        {
            get => this.model.ClientCert.ValidationMethod == ClientCertificateValidationMethod.SpecificIssuer;
            set
            {
                if (value)
                {
                    this.model.ClientCert.ValidationMethod = ClientCertificateValidationMethod.SpecificIssuer;
                }
            }
        }

        [NotifyModelChangedCollection(RequiresServiceRestart = true)]
        public BindableCollection<X509Certificate2ViewModel> TrustedIssuers { get; }

        [NotifyModelChangedCollection(RequiresServiceRestart = true)]
        public BindableCollection<string> RequiredEkus { get; }

        public X509Certificate2ViewModel SelectedIssuer { get; set; }

        public async Task AddIssuer()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "cer";
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "Certificate files (*.cer)|*.cer|All Files (*.*)|*.*";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog(this.GetWindow()) != true)
            {
                return;
            }

            try
            {
                var cert = new X509Certificate2(openFileDialog.FileName);
                var certstring = Convert.ToBase64String(cert.Export(X509ContentType.Cert));
                this.TrustedIssuers.Add(this.x509ViewModelFactory.CreateViewModel(cert));
                this.model.ClientCert.TrustedIssuers.Add(certstring);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Import error", $"Could not open the file\r\n{ex.Message}");
            }
        }

        public bool CanAddIssuer => this.ValidateSpecificIssuer;

        public async Task RemoveIssuer()
        {
            try
            {
                X509Certificate2ViewModel removing = this.SelectedIssuer;
                int position = this.TrustedIssuers.IndexOf(removing);
                this.TrustedIssuers.RemoveAt(position);
                this.model.ClientCert.TrustedIssuers.RemoveAt(position);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanRemoveIssuer => this.ValidateSpecificIssuer && this.SelectedIssuer != null;

        public string NewEku { get; set; }

        public string SelectedEku { get; set; }

        public void AddEku()
        {
            this.model.ClientCert.RequiredEkus.Add(this.NewEku);
            this.RequiredEkus.Add(this.NewEku);
            this.NewEku = null;
        }

        public bool CanAddEku
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.NewEku))
                {
                    return false;
                }

                return true;
            }
        }

        public void RemoveEku()
        {
            string removing = this.SelectedEku;
            this.NewEku = removing;
            this.RequiredEkus.Remove(removing);
            this.model.ClientCert.RequiredEkus.Remove(removing);
        }

        public async Task OktaHelp()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkAuthNSetupOkta);
        }

        public async Task AadHelp()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkAuthNSetupAzureAD);
        }

        public async Task AdfsHelp()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkAuthNSetupAdfs);
        }

        public bool CanRemoveEku => this.SelectedEku != null;

        public PackIconUniconsKind Icon => PackIconUniconsKind.User;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
