using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public class DiscoveryServices : IDiscoveryServices
    {
        public const int MaxRetry = 5;

        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainDnsCache = new ConcurrentDictionary<SecurityIdentifier, string>();
        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainNetBiosCache = new ConcurrentDictionary<SecurityIdentifier, string>();

        public void FindDcAndExecuteWithRetry(Action<string> action)
        {
            string domain = Domain.GetComputerDomain().Name;
            this.FindDcAndExecuteWithRetry(domain, t => { action(t); return true; });
        }

        public T FindDcAndExecuteWithRetry<T>(Func<string, T> action)
        {
            string domain = Domain.GetComputerDomain().Name;
            return this.FindDcAndExecuteWithRetry(domain, 0, action);
        }

        public void FindDcAndExecuteWithRetry(string domain, Action<string> action)
        {
            this.FindDcAndExecuteWithRetry(domain, t => { action(t); return true; });
        }

        public T FindDcAndExecuteWithRetry<T>(string domain, Func<string, T> action)
        {
            return this.FindDcAndExecuteWithRetry(domain, 0, action);
        }

        public T Find2012DcAndExecuteWithRetry<T>(string domain, Func<string, T> action)
        {
            return this.FindDcAndExecuteWithRetry(domain, DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED, action);
        }

        public T FindGcAndExecuteWithRetry<T>(string domain, Func<string, T> action)
        {
            return this.FindDcAndExecuteWithRetry(domain, DsGetDcNameFlags.DS_GC_SERVER_REQUIRED, action);
        }

        public T FindDcAndExecuteWithRetry<T>(string domain, DsGetDcNameFlags flags, Func<string, T> action)
        {
            int retryCount = 0;
            Exception lastException = null;
            HashSet<string> attemptedDCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (domain == null)
            {
                domain = Domain.GetComputerDomain().Name;
            }

            string dc = this.GetDomainController(domain, false);

            while (retryCount < MaxRetry && attemptedDCs.Add(dc))
            {
                try
                {
                    return action.Invoke(dc);
                }
                catch (COMException ex) when (ex.HResult == -2147016646) // Server is not operational
                {
                    lastException = ex;
                }
                catch (Win32Exception ex) when (ex.HResult == -2147467259) //RPC_NOT_AVAILABLE
                {
                    lastException = ex;
                }
                catch (Exception ex) when (ex.InnerException is Win32Exception we && we.HResult == -2147467259) //RPC_NOT_AVAILABLE
                {
                    lastException = ex;
                }

                dc = this.GetDomainController(domain, flags | DsGetDcNameFlags.DS_FORCE_REDISCOVERY);
                retryCount++;
            }

            if (lastException != null)
            {
                throw lastException;
            }
            else
            {
                throw new DirectoryException("Unable to execute command against DC");
            }
        }

        private string GetContextDn(string contextName, string dnsDomain)
        {
            return this.FindDcAndExecuteWithRetry(dnsDomain, dc =>
            {
                var rootDse = new DirectoryEntry($"LDAP://{dc}/rootDSE");

                var context = (string)rootDse.Properties[contextName]?.Value;

                if (context == null)
                {
                    throw new ObjectNotFoundException($"Naming context lookup failed for {contextName}");
                }

                return context;
            });
        }

        public DirectoryEntry GetConfigurationNamingContext(string dnsDomain)
        {
            return new DirectoryEntry($"LDAP://{this.GetContextDn("configurationNamingContext", dnsDomain)}");
        }

        public DirectoryEntry GetSchemaNamingContext(string dnsDomain)
        {
            return new DirectoryEntry($"LDAP://{this.GetContextDn("schemaNamingContext", dnsDomain)}");
        }

        public string GetDomainNameNetBios(SecurityIdentifier sid)
        {
            if (domainNetBiosCache.TryGetValue(sid.AccountDomainSid, out string value))
            {
                return value;
            }

            return this.FindDcAndExecuteWithRetry(dc =>
            {
                var result = NativeMethods.CrackNames(DsNameFormat.SecurityIdentifier, DsNameFormat.Nt4Name, sid.AccountDomainSid.ToString(), dc, null).Name.Trim('\\');
                domainNetBiosCache.TryAdd(sid.AccountDomainSid, result);

                return result;
            });
        }

        public string GetDomainNameDns(SecurityIdentifier sid)
        {
            if (domainDnsCache.TryGetValue(sid.AccountDomainSid, out string value))
            {
                return value;
            }

            return this.FindDcAndExecuteWithRetry(dc =>
            {
                var result = NativeMethods.CrackNames(DsNameFormat.SecurityIdentifier, DsNameFormat.DistinguishedName, sid.AccountDomainSid.ToString(), dc, null);
                domainDnsCache.TryAdd(sid.AccountDomainSid, result.Domain);

                return result.Domain;
            });
        }

        public string GetDomainNameDns(string dn)
        {
            return this.FindDcAndExecuteWithRetry(dc =>
            {
                var result = NativeMethods.CrackNames(DsNameFormat.DistinguishedName, DsNameFormat.DistinguishedName, dn, dc, null);
                return result.Domain;
            });
        }

        public string GetDomainController(string domainDns)
        {
            return NativeMethods.GetDomainControllerForDnsDomain(domainDns, false);
        }

        public string GetDomainController(string domainDns, bool forceRediscovery)
        {
            return NativeMethods.GetDomainControllerForDnsDomain(domainDns, forceRediscovery);
        }

        public string GetDomainController(string domainDns, DsGetDcNameFlags flags)
        {
            return NativeMethods.GetDomainControllerForDnsDomain(domainDns, flags);
        }

        public string GetDomainControllerFromDNOrDefault(string dn)
        {
            if (dn != null)
            {
                try
                {
                    return this.GetDomainControllerFromDN(dn);
                }
                catch
                {
                    // Ignore
                }
            }

            return Domain.GetComputerDomain().FindDomainController().Name;
        }

        public string GetDomainControllerFromDN(string dn)
        {
            string domain = this.GetDomainNameDns(dn);
            return this.GetDomainController(domain);
        }

        public string GetForestNameDns(string dn)
        {
            var domain = this.GetDomainNameDns(dn);

            if (domain != null)
            {
                var domainObject = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domain));
                return domainObject.Forest.Name;
            }

            return null;
        }

        public string GetFullyQualifiedAdsPath(string dn)
        {
            string server = this.GetDomainControllerFromDNOrDefault(dn);
            return $"LDAP://{server}/{dn}";
        }

        public string GetFullyQualifiedDomainControllerAdsPath(string dn)
        {
            string server = this.GetDomainControllerFromDNOrDefault(dn);
            return $"LDAP://{server}";
        }
    }
}
