using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell.Commands;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthenticationViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly AuthenticationOptions model;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IX509Certificate2ViewModelFactory x509ViewModelFactory;
        private readonly ILogger logger;
        private readonly RandomNumberGenerator rng;

        public AuthenticationViewModel(AuthenticationOptions model, ILogger<AuthenticationViewModel> logger, INotifiableEventPublisher eventPublisher, IDialogCoordinator dialogCoordinator, IX509Certificate2ViewModelFactory x509ViewModelFactory, RandomNumberGenerator rng)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.x509ViewModelFactory = x509ViewModelFactory;
            this.logger = logger;
            this.rng = rng;

            model.Iwa ??= new IwaAuthenticationProviderOptions();
            model.Oidc ??= new OidcAuthenticationProviderOptions();
            model.WsFed ??= new WsFedAuthenticationProviderOptions();
            model.ClientCert ??= new CertificateAuthenticationProviderOptions();
            model.ClientCert.TrustedIssuers ??= new List<string>();
            model.ClientCert.RequiredEkus ??= new List<string>();

            this.TrustedIssuers = new BindableCollection<X509Certificate2ViewModel>();
            this.BuildTrustedIssuers();
            this.RequiredEkus = new BindableCollection<string>(model.ClientCert.RequiredEkus);

            eventPublisher.Register(this);
        }

        private void BuildTrustedIssuers()
        {
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
                    this.logger.LogError(ex, $"The trusted issuer certificate at position {count} could not be parsed");
                }
            }
        }

        [NotifiableProperty]
        public AuthenticationMode AuthenticationMode { get => this.model.Mode; set => this.model.Mode = value; }

        public IEnumerable<AuthenticationMode> AuthenticationModeValues
        {
            get
            {
                return Enum.GetValues(typeof(AuthenticationMode)).Cast<AuthenticationMode>();
            }
        }

        [NotifiableProperty]
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

        [NotifiableProperty]
        public string OidcAuthority { get => this.model.Oidc.Authority; set => this.model.Oidc.Authority = value; }

        [NotifiableProperty]
        public string OidcClientID { get => this.model.Oidc.ClientID; set => this.model.Oidc.ClientID = value; }

        [NotifiableProperty]
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

                    this.model.Oidc.Secret = new EncryptedData();
                    this.model.Oidc.Secret.Mode = 1;
                    byte[] salt = new byte[128];
                    rng.GetBytes(salt);
                    this.model.Oidc.Secret.Salt = Convert.ToBase64String(salt);
                    this.model.Oidc.Secret.Data = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.LocalMachine));
                }
            }
        }

        public void OidcSecretFocus()
        {
            this.OidcSecret = null;
        }

        [NotifiableProperty]
        public string WsFedRealm { get => this.model.WsFed.Realm; set => this.model.WsFed.Realm = value; }

        [NotifiableProperty]
        public string WsFedMetadata { get => this.model.WsFed.Metadata; set => this.model.WsFed.Metadata = value; }

        [NotifiableProperty]
        public bool RequireSmartCardEku
        {
            get => this.model.ClientCert.RequireSmartCardLogonEku;
            set => this.model.ClientCert.RequireSmartCardLogonEku = value;
        }

        [NotifiableProperty]
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

        [NotifiableProperty]
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

        [NotifiableProperty]
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

        [NotifiableCollection]
        public BindableCollection<X509Certificate2ViewModel> TrustedIssuers { get; }

        [NotifiableCollection]
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

        public void RemoveIssuer()
        {
            X509Certificate2ViewModel removing = this.SelectedIssuer;
            int position = this.TrustedIssuers.IndexOf(removing);
            this.TrustedIssuers.RemoveAt(position);
            this.model.ClientCert.TrustedIssuers.RemoveAt(position);
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

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public bool CanRemoveEku => this.SelectedEku != null;

        public string DisplayName { get; set; } = "Authentication";

        public PackIconUniconsKind Icon => PackIconUniconsKind.User;

        public UIElement View { get; set; }
    }
}
