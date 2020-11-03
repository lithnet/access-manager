using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Lithnet.Security.Authorization;

namespace Lithnet.AccessManager.Server
{
    public class CertificatePermissionProvider : ICertificatePermissionProvider
    {
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly ILocalSam localSam;

        public CertificatePermissionProvider(IWindowsServiceProvider windowsServiceProvider, ILocalSam localSam)
        {
            this.windowsServiceProvider = windowsServiceProvider;
            this.localSam = localSam;
        }

        public void AddReadPermission(X509Certificate2 certificate)
        {
            //this.AddReadPermission(certificate, windowsServiceProvider.GetServiceAccount());
            this.AddReadPermission(certificate, windowsServiceProvider.ServiceSid);
            this.AddReadPermission(certificate, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, localSam.GetLocalMachineAuthoritySid(Environment.MachineName)));
        }

        public void AddReadPermission(X509Certificate2 certificate, IdentityReference identity)
        {
            this.AddReadPermission(certificate, identity, out _);
        }

        public void AddReadPermission(X509Certificate2 certificate, IdentityReference identity, out Action rollbackAction)
        {
            FileSecurity originalSecurity = certificate.GetPrivateKeySecurity();
            certificate.AddPrivateKeyReadPermission(identity);
            rollbackAction = () => certificate.SetPrivateKeySecurity(originalSecurity);
        }

        public void AddReadPermission(X509Store store, IdentityReference identity)
        {
            this.AddReadPermission(store, identity, null);
        }

        public void AddReadPermission(X509Store store, IdentityReference identity, List<Action> rollbackActions)
        {
            foreach (X509Certificate2 c in store.Certificates.OfType<X509Certificate2>().Where(t => t.HasPrivateKey))
            {
                try
                {
                    FileSecurity originalSecurity = c.GetPrivateKeySecurity();
                    c.AddPrivateKeyReadPermission(identity);
                    rollbackActions?.Add(() => c.SetPrivateKeySecurity(originalSecurity));
                }
                catch (CertificateNotFoundException)
                { }
            }
        }

        public void AddReadPermissionToServiceStore(IdentityReference identity)
        {
            this.AddReadPermissionToServiceStore(identity, null);
        }

        public void AddReadPermissionToServiceStore(IdentityReference identity, List<Action> rollbackActions)
        {
            X509Store store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadOnly);
            this.AddReadPermission(store, identity, rollbackActions);
        }

        public bool ServiceAccountHasPermission(X509Certificate2 cert)
        {
            return this.HasPermission(cert, this.windowsServiceProvider.GetServiceAccount());
        }

        public bool HasPermission(X509Certificate2 cert, SecurityIdentifier sid)
        {
            var security = cert.GetPrivateKeySecurity();
            using AuthorizationContext c = new AuthorizationContext(sid);
            GenericSecurityDescriptor sd = new RawSecurityDescriptor(security.GetSecurityDescriptorSddlForm(AccessControlSections.All));
            return c.AccessCheck(sd, (int)FileSystemRights.Read);
        }
    }
}
