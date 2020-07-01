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

namespace Lithnet.AccessManager.Server.UI
{
    public class HostingViewModel : Screen, IHaveDisplayName
    {
        private const string SddlTemplate = "D:(A;;GX;;;{0})";
        private const string ServiceName = "lithnetadminaccesservice";
        private readonly HostingOptions model;
        private readonly IDialogCoordinator dialogCoordinator;

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
        }

        public int HttpPort { get => this.model.HttpSys.HttpPort; set => this.model.HttpSys.HttpPort = value; }

        public int HttpsPort { get => this.model.HttpSys.HttpsPort; set => this.model.HttpSys.HttpsPort = value; }

        public string Hostname { get => this.model.HttpSys.Hostname; set => this.model.HttpSys.Hostname = value; }

        public bool IsEditing { get; set; }

        public bool IsReading => !this.IsEditing;

        public X509Certificate2 Certificate { get; set; }

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

        public IEnumerable<X509Certificate2> AvailableCertificates => this.GetAvailableCertificates();

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

        public void StopService()
        {
            using ServiceController controller = new ServiceController(ServiceName);
            if (controller.Status != ServiceControllerStatus.Stopped)
            {
                controller.Stop();
            }

            controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        }

        public void StartService()
        {
            using ServiceController controller = new ServiceController(ServiceName);
            if (controller.Status != ServiceControllerStatus.Running)
            {
                controller.Start();
            }

            controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
        }

        public void RestartService()
        {
            this.StopService();
            this.StartService();
        }

        public void CreateUrlReservation(string url, SecurityIdentifier sid)
        {
            UrlAcl.Create(url, string.Format(SddlTemplate, sid.ToString()));
        }

        public void DeleteUrlReservation(string url)
        {
            foreach (var acl in UrlAcl.GetAllBindings())
            {
                if (acl.Prefix == url)
                {
                    UrlAcl.Delete(acl.Prefix);
                }
            }
        }

        public X509Certificate2 GetCertificate()
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

        public void ReplaceCertificate(X509Certificate2 cert, int port)
        {
            // Grant access to private key

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

        public IEnumerable<X509Certificate2> GetAvailableCertificates()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            Oid serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");

            foreach (X509Certificate2 c in store.Certificates.OfType<X509Certificate2>().Where(t => t.HasPrivateKey))
            {
                foreach (X509EnhancedKeyUsageExtension x in c.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                {
                    foreach (Oid o in x.EnhancedKeyUsages)
                    {
                        if (o.Value == serverAuthOid.Value)
                        {
                            yield return c;
                        }
                    }
                }
            }
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
                return null;
            }

            NTAccount account = new NTAccount(this.ServiceAccountDisplayName);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        private void SetServiceAccount(string username, string password)
        {
            NativeMethods.ChangeServiceCredentials(ServiceName, username, password);
        }

        public void SaveHostingSettings()
        {
            this.StopService();

            if (this.HasServiceAccountChanged)
            {
                this.SetServiceAccount(this.ServiceAccountDisplayName, this.ServiceAccountPassword);

                // Update on disk ACLs
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

            if (this.HasCertificateChanged || this.HasHttpsPortChanged)
            {
                // Grant access to private key
                this.ReplaceCertificate(this.Certificate, this.HttpsPort);
            }


            // Commit config


            this.StartService();
        }
    }
}
