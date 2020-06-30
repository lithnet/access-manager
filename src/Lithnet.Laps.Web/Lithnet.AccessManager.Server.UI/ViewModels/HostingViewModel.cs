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

namespace Lithnet.AccessManager.Server.UI
{
    public class HostingViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly HostingOptions model;

        public HostingViewModel(HostingOptions model)
        {
            this.model = model;
        }

        public string HttpUrl { get => this.model.HttpSys.HttpUrl; set => this.model.HttpSys.HttpUrl = value; }

        public string HttpsUrl { get => this.model.HttpSys.HttpsUrl; set => this.model.HttpSys.HttpsUrl = value; }

        public X509Certificate2 Certificate { get => this.GetCertificate(); set => this.SetCertificate(value); }

        public IEnumerable<X509Certificate2> AvailableCertificates => this.GetAvailableCertificates();

        public string ServiceAccount { get; set; }

        public string CertificateExpiryText { get; set; }

        public bool ShowCertificateExpiryWarning { get; set; }

        public void StopService()
        {
        }

        public void StartService()
        {
        }

        public void RestartService()
        {
        }

        public X509Certificate2 GetCertificate()
        {
            // get from netsh?

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

        public void SetCertificate(X509Certificate2 cert)
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

            CertificateBinding binding = new CertificateBinding(cert.Thumbprint, "My", new IPEndPoint(IPAddress.Parse("0.0.0.0"), 443), HttpSysHostingOptions.AppId);

            config.Bind(binding);
        }

        public IEnumerable<X509Certificate2> GetAvailableCertificates()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            Oid serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");

            foreach (X509Certificate2 c in store.Certificates.OfType<X509Certificate2>().Where(t => t.HasPrivateKey))
            {
                foreach (X509EnhancedKeyUsageExtension x in c.Extensions.OfType< X509EnhancedKeyUsageExtension>())
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

        public string DisplayName { get; set; } = "Web hosting";
    }
}
