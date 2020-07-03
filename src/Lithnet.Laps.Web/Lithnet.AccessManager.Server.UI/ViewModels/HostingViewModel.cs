using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Newtonsoft.Json;
using Stylet;
using SslCertBinding.Net;
using System.Net;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using NLog.LayoutRenderers.Wrappers;
using System.IO;
using System.Security.AccessControl;
using PropertyChanged;
using System.Runtime.CompilerServices;

namespace Lithnet.AccessManager.Server.UI
{
    public class HostingViewModel : Screen, IHaveDisplayName
    {
        private const string SddlTemplate = "D:(A;;GX;;;{0})";
        private const string ServiceName = "lithnetadminaccesservice";
        private readonly HostingOptions model;
        private readonly IDialogCoordinator dialogCoordinator;

        private ServiceController controller;

        public HostingViewModel(HostingOptions model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.ActiveHttpPort = this.model.HttpSys.HttpPort;
            this.ActiveHttpsPort = this.model.HttpSys.HttpsPort;
            this.ActiveHostname = this.model.HttpSys.Hostname;
            this.ActiveCertificate = this.GetCertificate();
            this.ActiveServiceAccount = this.GetServiceAccount();
            this.ServiceAccount = this.ActiveServiceAccount;
            this.DisplayName = "Web hosting";

            try
            {
                controller = new ServiceController(ServiceName);
                this.ServiceStatus = controller.Status.ToString();
            }
            catch
            {
                this.ServiceStatus = "Error: service not found";
            }
        }

        public string ServiceStatus { get; set; }

        public int HttpPort { get => this.model.HttpSys.HttpPort; set => this.model.HttpSys.HttpPort = value; }

        public int HttpsPort { get => this.model.HttpSys.HttpsPort; set => this.model.HttpSys.HttpsPort = value; }

        public string Hostname { get => this.model.HttpSys.Hostname; set => this.model.HttpSys.Hostname = value; }

        public bool IsEditing { get; set; }

        public bool IsReading => !this.IsEditing;

        public X509Certificate2 Certificate { get; set; }

        public string CertificateDisplayName => this.Certificate.ToDisplayName();

        public SecurityIdentifier ServiceAccount { get; set; }

        public string ServiceAccountDisplayName { get; set; }

        public string ServiceAccountPassword { get; set; }

        private int ActiveHttpPort { get; set; }

        private int ActiveHttpsPort { get; set; }

        private string ActiveHostname { get; set; }

        private X509Certificate2 ActiveCertificate { get; set; }

        private SecurityIdentifier ActiveServiceAccount { get; set; }

        private bool HasHttpPortChanged => this.ActiveHttpPort != this.HttpPort;

        private bool HasHttpsPortChanged => this.ActiveHttpsPort != this.HttpsPort;

        private bool HasHostnameChanged => this.ActiveHostname != this.Hostname;

        private bool HasCertificateChanged => this.ActiveCertificate != this.Certificate;

        private bool HasServiceAccountChanged => this.ActiveServiceAccount != this.ServiceAccount ||
            this.ServiceAccountPassword != null;

        public string CertificateExpiryText
        {
            get
            {
                if (this.Certificate == null)
                {
                    return null;
                }

                TimeSpan remainingTime = this.Certificate.NotAfter.Subtract(DateTime.Now);

                if (remainingTime.Ticks < 0)
                {
                    return "The certificate has expired";
                }

                return string.Format("Certificate expires in {0} days", remainingTime.ToString("%d"));
            }
        }

        public bool ShowCertificateExpiryWarning => this.Certificate == null ? false : this.Certificate.NotAfter.AddDays(-30) >= DateTime.Now;

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

                    this.ServiceAccountDisplayName = o.MsDsPrincipalName;
                    this.ServiceAccount = o.Sid;
                }
                else
                {
                    using (PrincipalContext p = new PrincipalContext(ContextType.Machine))
                    {
                        var up = UserPrincipal.FindByIdentity(p, r.Username);

                        if (up == null)
                        {
                            throw new ObjectNotFoundException("The user could not be found");
                        }

                        this.ServiceAccountDisplayName = up.SamAccountName;
                        this.ServiceAccount = up.Sid;
                        var f = (NTAccount)up.Sid.Translate(typeof(NTAccount));
                    }
                }

                this.ServiceAccountPassword = r.Password;

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
            if (this.CanStopService)
            {
                controller.Stop();
                this.ServiceStatus = "Stopping";
            }

            await Task.Run(async () =>
            {
                try
                {
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not stop in the requested time");
                }
            })
           .ContinueWith((x) => this.ServiceStatus = controller.Status.ToString());
        }

        public async Task StartService()
        {
            if (this.CanStartService)
            {
                controller.Start();
                this.ServiceStatus = "Starting";
            }

            await Task.Run(async () =>
            {
                try
                {
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not start in the requested time");
                }
            })
             .ContinueWith((x) => this.ServiceStatus = controller.Status.ToString());
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

        private void ReplaceCertificate(X509Certificate2 cert, int port)
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

        private void AddPrivateKeyReadPermission(X509Certificate2 cert, SecurityIdentifier sid)
        {
            string location = NativeMethods.GetKeyLocation(cert);

            if (location == null)
            {
                throw new CertificateNotFoundException("The certificate private key was not found. Manually add permissions for the service account to read this private key");
            }

            AddFileSecurity(location, sid, FileSystemRights.Read, AccessControlType.Allow);
        }

        private static void AddFileSecurity(string fileName, IdentityReference account, FileSystemRights rights, AccessControlType controlType)
        {
            FileInfo info = new FileInfo(fileName);
            FileSecurity fSecurity = info.GetAccessControl();

            fSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, controlType));

            info.SetAccessControl(fSecurity);
        }

        private X509Certificate2 GetCertificateFromStore(string storeName, string thumbprint)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine, OpenFlags.ReadOnly);

            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)?[0];
        }

        private SecurityIdentifier GetServiceAccount()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{ServiceName}", false);

            if (key == null)
            {
                return null;
            }

            this.ServiceAccountDisplayName = key.GetValue("ObjectName") as string;

            if (this.ServiceAccountDisplayName == null)
            {
                this.ServiceAccountDisplayName = "LocalSystem";
            }

            NTAccount account = new NTAccount(this.ServiceAccountDisplayName);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        private void SetServiceAccount(string username, string password)
        {
            NativeMethods.ChangeServiceCredentials(ServiceName, username, password);
        }

        public async Task SaveHostingSettings()
        {
            await this.StopService();

            if (this.HasServiceAccountChanged || this.ServiceAccountPassword != null)
            {
                this.SetServiceAccount(this.ServiceAccountDisplayName, this.ServiceAccountPassword);
            }

            if (this.HasServiceAccountChanged)
            {
                // update on disk ACLs
            }

            if (this.HasHttpPortChanged || this.HasHttpsPortChanged || this.HasServiceAccountChanged || this.HasHostnameChanged)
            {
                string httpOld = HttpSysHostingOptions.BuildPrefix(this.ActiveHostname, this.ActiveHttpPort, this.model.HttpSys.Path, false);
                string httpsOld = HttpSysHostingOptions.BuildPrefix(this.ActiveHostname, this.ActiveHttpPort, this.model.HttpSys.Path, true);

                this.DeleteUrlReservation(httpOld);
                this.DeleteUrlReservation(httpsOld);

                this.CreateUrlReservation(this.model.HttpSys.BuildHttpUrlPrefix(), this.ServiceAccount);
                this.CreateUrlReservation(this.model.HttpSys.BuildHttpsUrlPrefix(), this.ServiceAccount);
            }

            if (this.HasCertificateChanged || this.HasServiceAccountChanged)
            {
                this.AddPrivateKeyReadPermission(this.Certificate, this.ServiceAccount);
            }

            if (this.HasCertificateChanged || this.HasHttpsPortChanged)
            {
                this.ReplaceCertificate(this.Certificate, this.HttpsPort);
            }


            // Commit config

            await this.StartService();
        }
    }
}

