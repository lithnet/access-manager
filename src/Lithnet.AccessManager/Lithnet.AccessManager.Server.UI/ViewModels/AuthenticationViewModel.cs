using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.ViewModels;
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
        private readonly IX509Certificate2ViewModelFactory x509ViewModelFactory;
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly RandomNumberGenerator rng;
        private readonly INotifiableEventPublisher eventPublisher;

        public AuthenticationViewModel(AuthenticationOptions model, ILogger<AuthenticationViewModel> logger, INotifiableEventPublisher eventPublisher, IDialogCoordinator dialogCoordinator, IX509Certificate2ViewModelFactory x509ViewModelFactory, RandomNumberGenerator rng, IDirectory directory)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.x509ViewModelFactory = x509ViewModelFactory;
            this.logger = logger;
            this.rng = rng;
            this.directory = directory;
            this.eventPublisher = eventPublisher;

            this.DisplayName = "Authentication";

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
            Task.Run(() =>
            {
                this.BuildAllowedToAuthenticateList();
                this.BuildTrustedIssuers();
                this.eventPublisher.Register(this);
            });
        }

        private void BuildAllowedToAuthenticateList()
        {
            this.AllowedPrincipals.Clear();

            foreach (var item in this.model.AllowedPrincipals)
            {
                if (item.TryParseAsSid(out SecurityIdentifier sid))
                {
                    SecurityIdentifierViewModel vm = new SecurityIdentifierViewModel
                    {
                        Sid = sid.ToString(),
                        DisplayName = this.GetSidDisplayName(sid)
                    };

                    this.AllowedPrincipals.Add(vm);
                }
            }
        }

        public SecurityIdentifierViewModel SelectedAllowedPrincipal { get; set; }

        public bool CanRemoveAllowedPrincipal => this.SelectedAllowedPrincipal != null;

        public void RemoveAllowedPrincipal()
        {
            SecurityIdentifierViewModel selected = this.SelectedAllowedPrincipal;

            if (selected == null)
            {
                return;
            }

            this.model.AllowedPrincipals.Remove(selected.Sid);
            this.AllowedPrincipals.Remove(selected);
        }

        public async Task AddAllowedPrincipal()
        {
            try
            {
                ExternalDialogWindow w = new ExternalDialogWindow();
                w.Title = "Select forest";
                var vm = new SelectForestViewModel();
                w.DataContext = vm;
                w.SaveButtonName = "Next...";
                w.SizeToContent = SizeToContent.WidthAndHeight;
                w.SaveButtonIsDefault = true;
                vm.AvailableForests = new List<string>();
                var domain = Domain.GetCurrentDomain();
                vm.AvailableForests.Add(domain.Forest.Name);
                vm.SelectedForest = domain.Forest.Name;

                foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
                {
                    if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                    {
                        vm.AvailableForests.Add(trust.TargetName);
                    }
                }

                w.Owner = this.GetWindow();

                if (!w.ShowDialog() ?? false)
                {
                    return;
                }

                DsopScopeInitInfo scope = new DsopScopeInitInfo();
                scope.Filter = new DsFilterFlags();

                scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS;

                scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;

                scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE;

                var targetServer = this.directory.GetDomainControllerForDomain(vm.SelectedForest ?? Forest.GetCurrentForest().Name);

                var result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

                if (result != null)
                {
                    byte[] sidraw = result.Attributes["objectSid"] as byte[];
                    if (sidraw == null)
                    {
                        return;
                    }

                    SecurityIdentifierViewModel sidvm = new SecurityIdentifierViewModel();
                    var sid = new SecurityIdentifier(sidraw, 0);
                    sidvm.Sid = sid.ToString();

                    if (this.model.AllowedPrincipals.Any(t => string.Equals(t, sidvm.Sid, StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    sidvm.DisplayName = this.GetSidDisplayName(sid);

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

        private string GetSidDisplayName(SecurityIdentifier sid)
        {
            try
            {
                NTAccount adminGroup = (NTAccount)sid.Translate(typeof(NTAccount));
                return adminGroup.Value;
            }
            catch
            {
                try
                {
                    return this.directory.TranslateName(sid.ToString(), AccessManager.Interop.DsNameFormat.SecurityIdentifier, AccessManager.Interop.DsNameFormat.Nt4Name);
                }
                catch
                {
                    return sid.ToString();
                }
            }
        }

        [NotifiableCollection]
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
                    this.model.Oidc.Secret.Data = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), salt, DataProtectionScope.LocalMachine));
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

        public async Task OktaHelp()
        {
            await this.OpenLink(Constants.HelpLinkAuthNSetupOkta);
        }

        public async Task AadHelp()
        {
            await this.OpenLink(Constants.HelpLinkAuthNSetupAzureAD);
        }

        public async Task AdfsHelp()
        {
            await this.OpenLink(Constants.HelpLinkAuthNSetupAdfs);
        }

        private async Task OpenLink(string link)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = link,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not open link");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not open the default link handler\r\n{ex.Message}");
            }
        }


        public bool CanRemoveEku => this.SelectedEku != null;

        public PackIconUniconsKind Icon => PackIconUniconsKind.User;
    }
}
