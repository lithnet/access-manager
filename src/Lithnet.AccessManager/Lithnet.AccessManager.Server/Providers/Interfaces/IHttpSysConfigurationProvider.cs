using Lithnet.AccessManager.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Server
{
    public interface IHttpSysConfigurationProvider
    {
        void CreateNewHttpReservations(HttpSysHostingOptions originalOptions, HttpSysHostingOptions newOptions, List<Action> rollbackActions);

        X509Certificate2 GetCertificate();

        bool IsReservationInUse(string newurl, out string user);

        void UpdateCertificateBinding(string thumbprint, int httpsPort, List<Action> rollbackActions);
    }
}