using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Printing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.Theming;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SslCertBinding.Net;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class HostingViewModel : Screen, IHaveDisplayName
    {
        private const string SddlTemplate = "D:(A;;GX;;;{0})";

        private HostingOptions OriginalModel { get; set; }

        private HostingOptions WorkingModel { get; set; }

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IServiceSettingsProvider serviceSettings;

        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly ILogger<HostingViewModel> logger;

        private readonly IAppPathProvider pathProvider;

        public HostingViewModel(HostingOptions model, IDialogCoordinator dialogCoordinator, IServiceSettingsProvider serviceSettings, ILogger<HostingViewModel> logger, IModelValidator<HostingViewModel> validator, IAppPathProvider pathProvider)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.OriginalModel = model;
            this.WorkingModel = this.CloneModel(model);
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettings = serviceSettings;
            this.Certificate = this.GetCertificate();
            this.OriginalCertificate = this.Certificate;
            this.ServiceAccount = this.serviceSettings.GetServiceAccount();
            this.OriginalServiceAccount = this.ServiceAccount;
            this.ServiceStatus = this.serviceSettings.ServiceController.Status.ToString();
            this.DisplayName = "Web hosting";
            this.cancellationTokenSource = new CancellationTokenSource();
            this.AutoValidate = false;
            this.Validator = validator;
            _ = this.PollServiceStatus(this.cancellationTokenSource.Token);
            _ = this.TryGetVersion();
        }

        private X509Certificate2 OriginalCertificate { get; set; }

        private SecurityIdentifier OriginalServiceAccount { get; set; }

        private string workingServiceAccountUserName { get; set; }

        private string workingServiceAccountPassword { get; set; }

        public string ServiceStatus { get; set; }

        public int HttpPort { get => this.WorkingModel.HttpSys.HttpPort; set => this.WorkingModel.HttpSys.HttpPort = value; }

        public int HttpsPort { get => this.WorkingModel.HttpSys.HttpsPort; set => this.WorkingModel.HttpSys.HttpsPort = value; }

        public string Hostname { get => this.WorkingModel.HttpSys.Hostname; set => this.WorkingModel.HttpSys.Hostname = value; }

        public bool IsEditing { get; set; }

        public string EditSettingsButtonText { get; set; } = "Edit settings";

        public async Task EditSettings()
        {
            if (!this.IsEditing)
            {
                this.EditSettingsButtonText = "Commit changes";
                this.IsEditing = true;
                this.AutoValidate = true;
                return;
            }

            bool updatePrivateKeyPermissions =
                this.ServiceAccount != this.OriginalServiceAccount ||
                this.Certificate?.Thumbprint != this.OriginalCertificate?.Thumbprint;

            bool updateHttpReservations =
                this.WorkingModel.HttpSys.Hostname != this.OriginalModel.HttpSys.Hostname ||
                this.WorkingModel.HttpSys.HttpPort != this.OriginalModel.HttpSys.HttpPort ||
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                this.ServiceAccount != this.OriginalServiceAccount;

            bool updateConfigFile = updateHttpReservations;

            bool updateCertificateBinding =
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                this.Certificate?.Thumbprint != this.OriginalCertificate?.Thumbprint;

            bool updateServiceAccount = this.workingServiceAccountUserName != null;

            try
            {
                if (updatePrivateKeyPermissions)
                {
                    this.Certificate.AddPrivateKeyReadPermission(this.ServiceAccount);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not add private key permissions");
                var result = await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred while trying to add permissions for the service account {this.ServiceAccountDisplayName} to read the private key of the specified certificate. Try adding permissions for this manually using the Windows computer certificates MMC console. Do you want to continue with the operation?\r\n{ex.Message}", MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Canceled)
                {
                    return;
                }
            }

            try
            {
                if (updateHttpReservations)
                {
                    this.CreateNewHttpReservations();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating HTTP reservations");
                this.TryRollbackHttpReservations();

                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not create the HTTP reservations\r\n{ex.Message}");
                return;
            }



            try
            {
                if (updateConfigFile)
                {
                    this.WorkingModel.Save(pathProvider.HostingConfigFile);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not save updated config file");
                this.TryRollbackConfig();
                return;
            }

            try
            {
                if (updateCertificateBinding)
                {
                    this.UpdateCertificateBinding();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error creating certificate binding");

                this.TryRollbackCertificateBinding();

                if (updateHttpReservations)
                {
                    this.TryRollbackHttpReservations();
                }

                if (updateConfigFile)
                {
                    this.TryRollbackConfig();
                }

                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not bind the certificate to the specified port\r\n{ex.Message}");

                return;
            }

            try
            {
                if (updateServiceAccount)
                {
                    this.serviceSettings.SetServiceAccount(this.workingServiceAccountUserName, this.workingServiceAccountPassword);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex,
                    "Could not change the service account to the specified account {serviceAccountName}",
                    workingServiceAccountUserName);

                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The service account could not be changed\r\n{ex.Message}");

                if (updateCertificateBinding)
                {
                    this.TryRollbackCertificateBinding();
                }

                if (updateHttpReservations)
                {
                    this.TryRollbackHttpReservations();
                }

                if (updateConfigFile)
                {
                    this.TryRollbackConfig();
                }

                return;
            }

            if (updateCertificateBinding || updateHttpReservations || updatePrivateKeyPermissions ||
                updateServiceAccount || updateConfigFile)
            {
                this.OriginalModel = this.CloneModel(this.WorkingModel);
                this.OriginalCertificate = this.Certificate;
                this.OriginalServiceAccount = this.ServiceAccount;

                this.ResetEditState();

                await this.dialogCoordinator.ShowMessageAsync(this, "Configuration updated", $"The service configuration has been updated. Restart the service for the new settings to take effect");
            }
            else
            {
                this.ResetEditState();
            }
        }

        public string CurrentVersion { get; set; }

        public string AvailableVersion { get; set; }

        public bool IsUpToDate { get; set; }

        public bool UpdateAvailable { get; set; }

        public string UpdateLink { get; set; }

        public void DownloadUpdate()
        {
            if (this.UpdateLink == null)
            {
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = this.UpdateLink,
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        public async Task TryGetVersion()
        {
            try
            {
                var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
                this.CurrentVersion = currentVersion.ToString();

                string appdata = await DownloadFile(Constants.UrlProductVersionInfo);
                if (appdata != null)
                {
                    var versionInfo = JsonConvert.DeserializeObject<PublishedVersionInfo>(appdata);

                    if (Version.TryParse(versionInfo.CurrentVersion, out Version onlineVersion))
                    {
                        this.AvailableVersion = onlineVersion.ToString();

                        if (onlineVersion > currentVersion)
                        {
                            this.UpdateAvailable = true;
                            this.IsUpToDate = false;
                            this.UpdateLink = versionInfo.UserUrl;
                        }
                        else
                        {
                            this.UpdateAvailable = false;
                            this.IsUpToDate = true;
                            this.UpdateLink = null;
                        }

                        return;
                    }
                }
            }
            catch { }

            this.UpdateAvailable = false;
            this.IsUpToDate = false;
            this.UpdateLink = null;
            this.AvailableVersion = null;

        }

        private static async Task<string> DownloadFile(string url)
        {
            using var client = new HttpClient();
            using var result = await client.GetAsync(url);

            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }

            return null;
        }

        private void TryRollbackConfig()
        {
            try
            {
                this.OriginalModel.Save(pathProvider.HostingConfigFile);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not rollback the config file");
            }
        }
        public void CancelSave()
        {
            this.ServiceAccount = this.OriginalServiceAccount;
            this.Certificate = this.OriginalCertificate;
            this.WorkingModel = this.OriginalModel;
            this.ResetEditState();
        }

        private void ResetEditState()
        {
            this.AutoValidate = false;
            this.ClearAllPropertyErrors();
            this.IsEditing = false;
            this.EditSettingsButtonText = "Edit settings";
            this.workingServiceAccountUserName = null;
            this.workingServiceAccountPassword = null;
        }

        public X509Certificate2 Certificate { get; set; }

        public string CertificateDisplayName => this.Certificate.ToDisplayName();

        public SecurityIdentifier ServiceAccount { get; set; }

        public string ServiceAccountDisplayName
        {
            get
            {
                try
                {
                    return this.ServiceAccount?.Translate(typeof(NTAccount))?.Value ?? "<not set>";
                }
                catch
                {
                    return this.ServiceAccount?.ToString() ?? "<not set>";
                }
            }
        }

        public string CertificateExpiryText { get; set; }

        public bool IsCertificateCurrent { get; set; }

        public bool IsCertificateExpiring { get; set; }

        public bool IsCertificateExpired { get; set; }

        public string Glyph { get; set; } = "";

        public PackIconMaterialKind Icon => PackIconMaterialKind.Web;

        public void OnCertificateChanged()
        {
            this.IsCertificateCurrent = false;
            this.IsCertificateExpired = false;
            this.IsCertificateExpiring = false;
            
            if (this.Certificate == null)
            {
                this.CertificateExpiryText = "Select a certificate";
                return;
            }

            TimeSpan remainingTime = this.Certificate.NotAfter.Subtract(DateTime.Now);

            if (remainingTime.Ticks <= 0)
            {
                this.IsCertificateExpired = true;
                this.CertificateExpiryText = "The certificate has expired";
                return;
            }

            if (remainingTime.TotalDays < 30)
            {
                this.IsCertificateExpiring = true;
            }
            else
            {
                this.IsCertificateCurrent = true;
            }

            this.CertificateExpiryText = $"Certificate expires in {remainingTime:%d} days";
        }

        public bool ShowCertificateExpiryWarning => this.Certificate != null && this.Certificate.NotAfter.AddDays(-30) >= DateTime.Now;

        public async Task SelectServiceAccountUser()
        {
            var r = await this.dialogCoordinator.ShowLoginAsync(this, "Service account", "Enter the credentials for the service account", new LoginDialogSettings
            {
                EnablePasswordPreview = true,
                AffirmativeButtonText = "OK"
            });


            if (r == null)
            {
                return;
            }

            try
            {
                ActiveDirectory directory = new ActiveDirectory();
                if (directory.TryGetPrincipal(r.Username, out ISecurityPrincipal o))
                {
                    if (o is IGroup)
                    {
                        throw new DirectoryException("The selected object must be a user");
                    }

                    this.ServiceAccount = o.Sid;
                }
                else
                {
                    using PrincipalContext p = new PrincipalContext(ContextType.Machine);
                    var up = UserPrincipal.FindByIdentity(p, r.Username);

                    if (up == null)
                    {
                        throw new ObjectNotFoundException("The user could not be found");
                    }

                    this.ServiceAccount = up.Sid;
                }

                this.workingServiceAccountUserName = r.Username;
                this.workingServiceAccountPassword = r.Password;
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The credentials provided could not be validated\r\n{ex.Message}", MessageDialogStyle.Affirmative);
            }
        }

        public bool CanStopService => this.ServiceStatus == ServiceControllerStatus.Running.ToString();

        public bool CanStartService => this.ServiceStatus == ServiceControllerStatus.Stopped.ToString();

        public async Task StopService()
        {
            try
            {
                if (this.CanStopService)
                {
                    this.serviceSettings.ServiceController.Stop();
                }
            }
            catch (Exception ex)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not stop service\r\n{ex.Message}");
                return;
            }
            try
            {
                await this.serviceSettings.ServiceController.WaitForStatusAsync(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30), CancellationToken.None);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not stop in the requested time");
            }
        }

        public async Task StartService()
        {
            try
            {
                if (this.CanStartService)
                {
                    this.serviceSettings.ServiceController.Start();
                }
            }
            catch (Exception ex)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not start service\r\n{ex.Message}");
                return;
            }

            try
            {
                await this.serviceSettings.ServiceController.WaitForStatusAsync(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30), CancellationToken.None);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not start in the requested time");
            }
        }

        public async Task RestartService()
        {
            await this.StopService();
            await this.StartService();
        }

        private void CreateUrlReservation(string url, SecurityIdentifier sid)
        {
            UrlAcl.Create(url, string.Format(SddlTemplate, sid.ToString()));
        }

        private void DeleteUrlReservation(string url)
        {
            foreach (var acl in UrlAcl.GetAllBindings())
            {
                if (acl.Prefix == url)
                {
                    UrlAcl.Delete(acl.Prefix);
                }
            }
        }

        public bool CanShowCertificateDialog => this.Certificate != null;

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.Certificate, this.GetHandle());
        }

        public void ShowSelectCertificateDialog()
        {
            X509Certificate2Collection results = X509Certificate2UI.SelectFromCollection(this.GetAvailableCertificateCollection(), "Select TLS certificate", "Select a certificate to use as the TLS certificate for this web site", X509SelectionFlag.SingleSelection, this.GetHandle());

            if (results.Count == 1)
            {
                this.Certificate = results[0];
            }
        }


        public void ShowImportDialog()
        {
            X509Certificate2 newCert = NativeMethods.ShowCertificateImportDialog(this.GetHandle(), "Import certificate", StoreLocation.LocalMachine, StoreName.My);

            if (newCert != null)
            {
                this.Certificate = newCert;
            }
        }


        private X509Certificate2 GetCertificate()
        {
            foreach (CertificateBinding binding in this.GetCertificateBindings())
            {
                if (binding.AppId == HttpSysHostingOptions.AppId)
                {
                    return this.GetCertificateFromStore(binding.StoreName, binding.Thumbprint);
                }
            }

            return null;
        }

        private List<CertificateBinding> GetCertificateBindings()
        {
            var config = new CertificateBindingConfiguration();
            var results = config.Query();

            return results.ToList();
        }

        private void UpdateCertificateBinding()
        {
            this.ReplaceCertificateBinding(this.Certificate, this.WorkingModel.HttpSys.HttpsPort);
        }

        private void TryRollbackCertificateBinding()
        {
            try
            {
                this.ReplaceCertificateBinding(this.OriginalCertificate, this.OriginalModel.HttpSys.HttpsPort);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to rollback certificate binding");
            }
        }

        private void ReplaceCertificateBinding(X509Certificate2 cert, int port)
        {
            var config = new CertificateBindingConfiguration();

            foreach (var b in config.Query())
            {
                if (b.AppId == HttpSysHostingOptions.AppId)
                {
                    config.Delete(b.IpPort);
                }
            }

            this.CreateCertificateBinding(cert, port);
        }

        private void CreateCertificateBinding(X509Certificate2 cert, int port)
        {
            var config = new CertificateBindingConfiguration();
            CertificateBinding binding = new CertificateBinding(cert.Thumbprint, "My", new IPEndPoint(IPAddress.Parse("0.0.0.0"), port), HttpSysHostingOptions.AppId);
            config.Bind(binding);
        }

        private X509Certificate2Collection GetAvailableCertificateCollection()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            Oid serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");

            foreach (X509Certificate2 c in store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false).OfType<X509Certificate2>().Where(t => t.HasPrivateKey))
            {
                foreach (X509EnhancedKeyUsageExtension x in c.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                {
                    foreach (Oid o in x.EnhancedKeyUsages)
                    {
                        if (o.Value == serverAuthOid.Value)
                        {
                            certs.Add(c);
                        }
                    }
                }
            }

            return certs;
        }

        private X509Certificate2 GetCertificateFromStore(string storeName, string thumbprint)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine, OpenFlags.ReadOnly);

            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)?[0];
        }


        private void CreateNewHttpReservations()
        {
            if (this.ServiceAccount == null)
            {
                return;
            }

            string httpOld = HttpSysHostingOptions.BuildPrefix(this.OriginalModel.HttpSys.Hostname, this.OriginalModel.HttpSys.HttpPort, this.OriginalModel.HttpSys.Path, false);
            string httpsOld = HttpSysHostingOptions.BuildPrefix(this.OriginalModel.HttpSys.Hostname, this.OriginalModel.HttpSys.HttpsPort, this.OriginalModel.HttpSys.Path, true);

            this.DeleteUrlReservation(httpOld);
            this.DeleteUrlReservation(httpsOld);

            this.CreateUrlReservation(this.WorkingModel.HttpSys.BuildHttpUrlPrefix(), this.ServiceAccount);
            this.CreateUrlReservation(this.WorkingModel.HttpSys.BuildHttpsUrlPrefix(), this.ServiceAccount);
        }

        private void TryRollbackHttpReservations()
        {
            try
            {

                string httpOld = HttpSysHostingOptions.BuildPrefix(this.OriginalModel.HttpSys.Hostname,
                    this.OriginalModel.HttpSys.HttpPort, this.OriginalModel.HttpSys.Path, false);
                string httpsOld = HttpSysHostingOptions.BuildPrefix(this.OriginalModel.HttpSys.Hostname,
                    this.OriginalModel.HttpSys.HttpsPort, this.OriginalModel.HttpSys.Path, true);

                this.DeleteUrlReservation(this.WorkingModel.HttpSys.BuildHttpUrlPrefix());
                this.DeleteUrlReservation(this.WorkingModel.HttpSys.BuildHttpsUrlPrefix());

                this.CreateUrlReservation(httpOld, this.ServiceAccount);
                this.CreateUrlReservation(httpsOld, this.ServiceAccount);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to rollback HTTP reservations");
            }
        }

        private async Task PollServiceStatus(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
                    this.serviceSettings.ServiceController.Refresh();
                    this.ServiceStatus = this.serviceSettings.ServiceController.Status.ToString();

                }
            }
            catch { }
        }


        private T CloneModel<T>(T model)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(model));
        }

    }
}

