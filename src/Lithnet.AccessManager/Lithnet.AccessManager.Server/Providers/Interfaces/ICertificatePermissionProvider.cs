using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public interface ICertificatePermissionProvider
    {
        void AddReadPermission(X509Certificate2 certificate, IdentityReference identity);

        void AddReadPermission(X509Store store, IdentityReference identity);

        void AddReadPermissionToServiceStore(IdentityReference identity);

        void AddReadPermission(X509Certificate2 certificate, IdentityReference identity, out Action rollbackAction);

        void AddReadPermission(X509Store store, IdentityReference identity, List<Action> rollbackActions);

        void AddReadPermissionToServiceStore(IdentityReference identity, List<Action> rollbackActions);
    }
}