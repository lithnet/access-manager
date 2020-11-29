using System;
using System.DirectoryServices;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public interface IDiscoveryServices
    {
        void FindDcAndExecuteWithRetry(Action<string> action);

        T FindDcAndExecuteWithRetry<T>(Func<string, T> action);

        T Find2012DcAndExecuteWithRetry<T>(string domain, Func<string, T> action);

        void FindDcAndExecuteWithRetry(string domain, Action<string> action);

        T FindDcAndExecuteWithRetry<T>(string domain, Func<string, T> action);

        T FindDcAndExecuteWithRetry<T>(string domain, DsGetDcNameFlags flags, Func<string, T> action);

        T FindDcAndExecuteWithRetry<T>(string server, string domain, DsGetDcNameFlags flags, DcLocatorMode mode, Func<string, T> action);

        T FindGcAndExecuteWithRetry<T>(string domain, Func<string, T> action);

        DirectoryEntry GetConfigurationNamingContext(string dnsDomain);

        string GetComputerSiteNameRpc(string computerName);

        bool TryGetComputerSiteNameRpc(string computerName, out string siteName);

        string GetDomainController(string domainDns);

        string GetDomainController(string domainDns, bool forceRediscovery);

        string GetDomainController(string server, string domainDns, DsGetDcNameFlags flags);

        string GetDomainControllerFromDN(string dn);

        string GetDomainControllerFromDNOrDefault(string dn);

        string GetDomainNameDns();

        string GetDomainNameDns(SecurityIdentifier sid);

        string GetDomainNameDns(string dn);

        string GetDomainNameNetBios(SecurityIdentifier sid);

        string GetDomainNameNetBios(string domainDns);

        string GetForestNameDns(string dn);

        string GetForestNameDns();

        string GetFullyQualifiedAdsPath(string dn);

        string GetFullyQualifiedRootAdsPath(string dn);

        DirectoryEntry GetSchemaNamingContext(string dnsDomain);

        bool DoesSchemaAttributeExist(string dnsDomain, string attributeName);

        bool DoesSchemaAttributeExist(string dnsDomain, string attributeName, bool refreshCache);

        Guid? GetSchemaAttributeGuid(string dnsDomain, string attributeName);

        Guid? GetSchemaAttributeGuid(string dnsDomain, string attributeName, bool refreshCache);

        Guid? GetSchemaObjectGuid(string dnsDomain, string objectName);

        Guid? GetSchemaObjectGuid(string dnsDomain, string objectName, bool refreshCache);
    }
}