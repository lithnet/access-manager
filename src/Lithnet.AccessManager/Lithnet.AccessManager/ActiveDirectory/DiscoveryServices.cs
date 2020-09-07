using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class DiscoveryServices : IDiscoveryServices
    {
        public const int MaxRetry = 5;

        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainDnsCache = new ConcurrentDictionary<SecurityIdentifier, string>();
        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainNetBiosCache = new ConcurrentDictionary<SecurityIdentifier, string>();
        private static readonly ConcurrentDictionary<string, string> dcCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger logger;

        public DiscoveryServices(ILogger<DiscoveryServices> logger)
        {
            this.logger = logger;
        }

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
            return this.FindDcAndExecuteWithRetry(null, domain, flags, action);
        }

        public T FindDcAndExecuteWithRetry<T>(string server, string domain, DsGetDcNameFlags flags, Func<string, T> action)
        {
            int retryCount = 0;
            Exception lastException = null;
            HashSet<string> attemptedDCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (domain == null)
            {
                domain = Domain.GetComputerDomain().Name;
            }

            string dc = this.GetDomainController(server, domain, flags);

            while (retryCount < MaxRetry && attemptedDCs.Add(dc))
            {
                //this.logger.LogTrace("Attempting to execute operation in domain {domain} against DC {dc}", domain, dc);

                try
                {
                    return action.Invoke(dc);
                }
                catch (COMException ex) when (ex.HResult == -2147016646) // Server is not operational
                {
                    lastException = ex;
                }
                catch (Win32Exception we) when (we.HResult == -2147467259            // RPC_NOT_AVAILABLE 
                                                || we.NativeErrorCode == 0x000020E1  // ERROR_DS_GCVERIFY_ERROR 
                                                || we.NativeErrorCode == 0x0000200E  // ERROR_DS_BUSY
                                                || we.NativeErrorCode == 0x0000200F  // ERROR_DS_UNAVAILABLE
                                                || we.NativeErrorCode == 0x0000203A  // ERROR_DS_SERVER_DOWN
                                                )
                {
                    lastException = we;
                }
                catch (Exception ex) when (ex.InnerException is Win32Exception we &&
                                           (we.HResult == -2147467259            // RPC_NOT_AVAILABLE 
                                            || we.NativeErrorCode == 0x000020E1  // ERROR_DS_GCVERIFY_ERROR 
                                            || we.NativeErrorCode == 0x0000200E  // ERROR_DS_BUSY
                                            || we.NativeErrorCode == 0x0000200F  // ERROR_DS_UNAVAILABLE
                                            || we.NativeErrorCode == 0x0000203A  // ERROR_DS_SERVER_DOWN
                                            ))
                {
                    lastException = ex;
                }

                this.logger.LogTrace(lastException, "Operation failed in domain {domain} against DC {dc} due to retry-able error", domain, dc);

                dc = this.GetDomainController(server, domain, flags | DsGetDcNameFlags.DS_FORCE_REDISCOVERY);
                retryCount++;
            }

            dcCache.TryRemove(BuildDcCacheKey(server, domain, flags), out _);

            if (lastException != null)
            {
                throw lastException;
            }
            else
            {
                throw new DirectoryException("Unable to execute command against DC");
            }
        }

        private static string BuildDcCacheKey(string server, string domain, DsGetDcNameFlags flags)
        {
            return $"{server}{domain}{flags}";
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

        public string GetComputerSiteName(string computerName)
        {
            return NativeMethods.GetComputerSiteName(computerName);
        }

        public bool TryGetComputerSiteName(string computerName, out string siteName)
        {
            siteName = null;
            try
            {
                siteName = this.GetComputerSiteName(computerName);
                return true;
            }
            catch
            {
                return false;
            }
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
            return this.GetDomainController(null, domainDns, DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED);
        }

        public string GetDomainController(string domainDns, bool forceRediscovery)
        {
            return this.GetDomainController(null, domainDns, DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED | DsGetDcNameFlags.DS_FORCE_REDISCOVERY);
        }

        public string GetDomainController(string server, string domainDns, DsGetDcNameFlags flags)
        {
            string key = BuildDcCacheKey(server, domainDns, flags);

            if (flags.HasFlag(DsGetDcNameFlags.DS_FORCE_REDISCOVERY))
            {
                return dcCache.AddOrUpdate(
                    key,
                    a => this.GetDc(server, domainDns, flags),
                    (a, b) =>
                    {
                        this.logger.LogTrace("New DC requested");
                        return this.GetDc(server, domainDns, flags);
                    });
            }

            return dcCache.GetOrAdd(key, k => this.GetDc(server, domainDns, flags));
        }

        private string GetDc(string server, string domainDns, DsGetDcNameFlags flags)
        {
            string dc;

            try
            {
                this.logger.LogTrace("Finding domain controller for server {server}, domain {domainDns} with flags {flags}", server, domainDns, flags.ToString());
                dc = NativeMethods.GetDomainControllerForDnsDomain(server, domainDns, null, flags);
                this.logger.LogTrace("DC locator found DC {dc} for domain {domainDns}, with flags {flags}", dc, domainDns, flags.ToString());
            }
            catch (DirectoryException dex) when (dex.InnerException is Win32Exception wex && wex.NativeErrorCode == 1722 && server != null)
            {
                this.logger.LogWarning(dex, "Could not connect to server {server} to find DC, local machine will be used to find a DC", server);
                dc = NativeMethods.GetDomainControllerForDnsDomain(null, domainDns, null, flags);
                this.logger.LogTrace("DC locator found DC {dc} for domain {domainDns}, with flags {flags}", dc, domainDns, flags.ToString());
            }

            return dc;
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
