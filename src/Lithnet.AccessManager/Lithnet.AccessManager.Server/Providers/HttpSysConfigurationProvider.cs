using Lithnet.AccessManager.Server.Configuration;
using SslCertBinding.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    public class HttpSysConfigurationProvider : IHttpSysConfigurationProvider
    {
        private const string SddlTemplate = "D:(A;;GX;;;{0})";

        private readonly IRegistryProvider registryProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;

        public HttpSysConfigurationProvider(IRegistryProvider registryProvider, IWindowsServiceProvider windowsServiceProvider)
        {
            this.registryProvider = registryProvider;
            this.windowsServiceProvider = windowsServiceProvider;
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
            CertificateBindingConfiguration config = new CertificateBindingConfiguration();
            CertificateBinding[] results = config.Query();

            return results.ToList();
        }


        private CertificateBinding GetCertificateBinding(CertificateBindingConfiguration config)
        {
            foreach (CertificateBinding binding in config.Query())
            {
                if (binding.AppId == HttpSysHostingOptions.AppId)
                {
                    return binding;
                }
            }

            return null;
        }

        public void UpdateCertificateBinding(string thumbprint, int httpsPort, List<Action> rollbackActions)
        {
            CertificateBindingConfiguration bindingConfiguration = new CertificateBindingConfiguration();
            CertificateBinding originalBinding = this.GetCertificateBinding(bindingConfiguration);

            if (originalBinding != null)
            {
                bindingConfiguration.Delete(originalBinding.IpPort);
                rollbackActions.Add(() => bindingConfiguration.Bind(originalBinding));
            }

            CertificateBinding binding = new CertificateBinding(thumbprint, "My", new IPEndPoint(IPAddress.Parse("0.0.0.0"), httpsPort), HttpSysHostingOptions.AppId, new BindingOptions());
            bindingConfiguration.Bind(binding);
            rollbackActions.Add(() => bindingConfiguration.Delete(binding.IpPort));

            this.registryProvider.CertBinding = binding.IpPort.ToString();
            rollbackActions.Add(() => this.registryProvider.CertBinding = originalBinding?.IpPort?.ToString());
        }

        private X509Certificate2 GetCertificateFromStore(string storeName, string thumbprint)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine, OpenFlags.ReadOnly);

            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false).OfType<X509Certificate2>().FirstOrDefault();
        }

        public void CreateNewHttpReservations(HttpSysHostingOptions originalOptions, HttpSysHostingOptions newOptions, List<Action> rollbackActions)
        {
            string httpOld = originalOptions.BuildHttpUrlPrefix();
            string httpsOld = originalOptions.BuildHttpsUrlPrefix();
            string httpNew = newOptions.BuildHttpUrlPrefix();
            string httpsNew = newOptions.BuildHttpsUrlPrefix();

            UrlAcl existingHttpOld = this.GetUrlReservation(httpOld);
            if (existingHttpOld != null)
            {
                UrlAcl.Delete(existingHttpOld.Prefix);
                rollbackActions.Add(() => UrlAcl.Create(existingHttpOld.Prefix, existingHttpOld.Sddl));
            }

            UrlAcl existingHttpsOld = this.GetUrlReservation(httpsOld);
            if (existingHttpsOld != null)
            {
                UrlAcl.Delete(existingHttpsOld.Prefix);
                rollbackActions.Add(() => UrlAcl.Create(existingHttpsOld.Prefix, existingHttpsOld.Sddl));
            }

            UrlAcl existingHttpNew = this.GetUrlReservation(httpNew);
            if (existingHttpNew != null)
            {
                UrlAcl.Delete(existingHttpNew.Prefix);
                rollbackActions.Add(() => UrlAcl.Create(existingHttpNew.Prefix, existingHttpNew.Sddl));
            }

            UrlAcl existingHttpsNew = this.GetUrlReservation(httpsNew);
            if (existingHttpsNew != null)
            {
                UrlAcl.Delete(existingHttpsNew.Prefix);
                rollbackActions.Add(() => UrlAcl.Create(existingHttpsNew.Prefix, existingHttpsNew.Sddl));
            }

            this.CreateUrlReservation(httpNew, this.windowsServiceProvider.ServiceSid);
            rollbackActions.Add(() => UrlAcl.Delete(httpNew));
            this.registryProvider.HttpAcl = httpNew;

            rollbackActions.Add(() => this.registryProvider.HttpAcl = httpOld);

            this.CreateUrlReservation(httpsNew, this.windowsServiceProvider.ServiceSid);
            rollbackActions.Add(() => UrlAcl.Delete(httpsNew));
            this.registryProvider.HttpsAcl = httpsNew;
            rollbackActions.Add(() => this.registryProvider.HttpsAcl = httpsOld);
        }

        private void CreateUrlReservation(string url, SecurityIdentifier sid)
        {
            UrlAcl.Create(url, string.Format(SddlTemplate, sid));
        }

        private UrlAcl GetUrlReservation(string url)
        {
            foreach (UrlAcl acl in UrlAcl.GetAllBindings())
            {
                if (string.Equals(acl.Prefix, url, StringComparison.OrdinalIgnoreCase))
                {
                    return acl;
                }
            }

            return null;
        }

        public bool IsReservationInUse(string newurl, out string user)
        {
            user = null;

            UrlAcl acl = this.GetUrlReservation(newurl);

            if (acl == null)
            {
                return false;
            }

            SecurityIdentifier currentOwner = null;

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, acl.Sddl);
            foreach (CommonAce dacl in sd.DiscretionaryAcl.OfType<CommonAce>())
            {
                if (dacl.SecurityIdentifier == this.windowsServiceProvider.ServiceSid)
                {
                    return false;
                }

                currentOwner = dacl.SecurityIdentifier;
            }

            if (currentOwner == null)
            {
                return false;
            }

            try
            {
                user = ((NTAccount)currentOwner.Translate(typeof(NTAccount))).Value;
            }
            catch
            {
                user = currentOwner.ToString();
            }

            return true;
        }
    }
}
