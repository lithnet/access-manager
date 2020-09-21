using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace Lithnet.AccessManager
{
    public class DiscoveryServices : IDiscoveryServices
    {
        public const int MaxRetry = 5;

        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainDnsCache = new ConcurrentDictionary<SecurityIdentifier, string>();
        private static readonly ConcurrentDictionary<SecurityIdentifier, string> domainNetBiosCache = new ConcurrentDictionary<SecurityIdentifier, string>();
        private static readonly ConcurrentDictionary<string, string> dcCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Guid?> attributeGuidCache = new ConcurrentDictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, Guid?> objectGuidCache = new ConcurrentDictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        private static Domain currentDomain;
        private static Forest currentForest;

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
            return this.FindDcAndExecuteWithRetry(null, domain, flags, DcLocatorMode.LocalDcLocator, action);
        }

        public T FindDcAndExecuteWithRetry<T>(string server, string domain, DsGetDcNameFlags flags, DcLocatorMode mode, Func<string, T> action)
        {
            int retryCount = 0;
            Exception lastException = null;
            HashSet<string> attemptedDCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (domain == null)
            {
                domain = Domain.GetComputerDomain().Name;
            }

            DcLocatorMode cachedMode = mode;

            string dc = this.GetDomainController(server, domain, flags, ref cachedMode);

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

                dc = this.GetDomainController(server, domain, flags | DsGetDcNameFlags.DS_FORCE_REDISCOVERY, ref cachedMode);
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

        public string GetComputerSiteNameRpc(string computerName)
        {
            return NativeMethods.GetComputerSiteName(computerName);
        }

        public bool TryGetComputerSiteNameRpc(string computerName, out string siteName)
        {
            siteName = null;
            try
            {
                siteName = this.GetComputerSiteNameRpc(computerName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetComputerSiteNameManual(string dnsDomain, string dnsHostName)
        {
            this.logger.LogTrace("Attempting to query IP addresses for {host}", dnsHostName);
            IPAddress[] ipAddresses;

            try
            {
                ipAddresses = Dns.GetHostAddresses(dnsHostName);
            }
            catch (Exception ex)
            {
                this.logger.LogTrace(ex, "Unable to resolve DNS entry for {host}", dnsHostName);
                return null;
            }

            if (ipAddresses.Length == 0)
            {
                return null;
            }

            this.logger.LogTrace("Host {host} has addresses {addresses}", dnsHostName, string.Join(", ", ipAddresses.Select(t => t.ToString())));

            List<Ws2_32.SOCKADDR> resolvedAddresses = new List<Ws2_32.SOCKADDR>();

            Win32Error result;

            foreach (IPAddress ipAddress in ipAddresses.OrderBy(t => (int)t.AddressFamily))
            {
                Ws2_32.SOCKADDR socketAddress;

                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    socketAddress = new Ws2_32.SOCKADDR_IN();
                }
                else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    socketAddress = new Ws2_32.SOCKADDR_IN6();
                }
                else
                {
                    this.logger.LogTrace("Ignoring unknown address type {ipAddressFamily}", ipAddress.AddressFamily.ToString());
                    continue;
                }

                int length = socketAddress.Size;
                string addr = ipAddress.ToString();

                result = Ws2_32.WSAStringToAddress(addr, (Ws2_32.ADDRESS_FAMILY)ipAddress.AddressFamily, IntPtr.Zero, socketAddress, ref length);

                if (result.Failed)
                {
                    this.logger.LogTrace(result.GetException(), "WSAStringToAddress failed");
                }

                resolvedAddresses.Add(socketAddress);
            }

            if (resolvedAddresses.Count == 0)
            {
                return null;
            }

            var socketAddresses = new Ws2_32.SOCKET_ADDRESS[resolvedAddresses.Count];
            for (int i = 0; i < resolvedAddresses.Count; i++)
            {
                socketAddresses[i].iSockaddrLength = resolvedAddresses[i].Size;
                socketAddresses[i].lpSockaddr = resolvedAddresses[i].DangerousGetHandle();
            }

            NetApi32.SafeNetApiBuffer siteNames = null;

            this.FindDcAndExecuteWithRetry(dnsDomain, dc =>
            {
                result = NetApi32.DsAddressToSiteNames(dc, (uint)socketAddresses.Length, socketAddresses, out siteNames);
                result.ThrowIfFailed("DsAddressToSiteNames failed");
            });

            if (siteNames == null || siteNames.IsInvalid)
            {
                return null;
            }

            List<string> sites = siteNames.ToStringEnum(resolvedAddresses.Count).ToList();
            string site = sites.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

            if (site == null)
            {
                this.logger.LogTrace("There was no site found for host {host} in domain {domain}", dnsHostName, dnsDomain);
                return null;
            }

            this.logger.LogTrace("Selecting site {site} from site list {sites}", site, string.Join(", ", sites));

            return site;
        }

        public string GetDomainNameNetBios(string domainDns)
        {
            return NativeMethods.GetNetbiosNameForDomain(domainDns);
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

        public string GetDomainNameDns()
        {
            if (currentDomain == null)
            {
                currentDomain = Domain.GetComputerDomain();
            }

            return currentDomain.Name;
        }

        public string GetForestNameDns()
        {
            if (currentForest == null)
            {
                currentForest = Domain.GetComputerDomain().Forest;
            }

            return currentForest.Name;
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
            DcLocatorMode mode = DcLocatorMode.LocalDcLocator;
            return this.GetDomainController(null, domainDns, DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED, ref mode);
        }

        public string GetDomainController(string domainDns, bool forceRediscovery)
        {
            DcLocatorMode mode = DcLocatorMode.LocalDcLocator;
            return this.GetDomainController(null, domainDns, DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED | DsGetDcNameFlags.DS_FORCE_REDISCOVERY, ref mode);
        }

        public string GetDomainController(string server, string domainDns, DsGetDcNameFlags flags)
        {
            DcLocatorMode mode = DcLocatorMode.LocalDcLocator;
            return this.GetDomainController(null, domainDns, flags, ref mode);
        }

        private string GetDomainController(string server, string domainDns, DsGetDcNameFlags flags, ref DcLocatorMode mode)
        {
            string key = BuildDcCacheKey(server, domainDns, flags);

            DcLocatorMode mode2 = mode;

            try
            {
                if (flags.HasFlag(DsGetDcNameFlags.DS_FORCE_REDISCOVERY))
                {
                    return dcCache.AddOrUpdate(
                        key,
                        a => this.GetDc(server, domainDns, flags, ref mode2),
                        (a, b) =>
                        {
                            this.logger.LogTrace("New DC requested");
                            return this.GetDc(server, domainDns, flags, ref mode2);
                        });
                }

                return dcCache.GetOrAdd(key, k => this.GetDc(server, domainDns, flags, ref mode2));
            }
            finally
            {
                mode = mode2;
            }
        }

        public string GetDc(string server, string domainDns, DsGetDcNameFlags flags, ref DcLocatorMode mode)
        {
            string dc;

            if (server != null)
            {
                bool hadNextClosestSite = flags.HasFlag(DsGetDcNameFlags.DS_TRY_NEXTCLOSEST_SITE);

                if (mode.HasFlag(DcLocatorMode.RemoteDcLocator))
                {
                    try
                    {
                        flags |= DsGetDcNameFlags.DS_TRY_NEXTCLOSEST_SITE;
                        this.logger.LogTrace("Remote DCLocator: Finding domain controller for server {server}, domain {domainDns} with flags {flags}", server, domainDns, flags.ToString());
                        dc = NativeMethods.GetDomainControllerForDnsDomain(server, domainDns, null, flags);
                        this.logger.LogTrace("Remote DCLocator: Found DC {dc} for server {server} in domain {domainDns}, with flags {flags}", dc, server, domainDns, flags.ToString());
                        return dc;
                    }
                    catch (DirectoryException dex) when (dex.InnerException is Win32Exception wex && wex.NativeErrorCode == 1722)
                    {
                        mode &= ~DcLocatorMode.RemoteDcLocator;
                        this.logger.LogWarning(dex, "Could not connect to server {server} to find DC", server);
                    }
                }

                if (!hadNextClosestSite)
                {
                    flags &= ~DsGetDcNameFlags.DS_TRY_NEXTCLOSEST_SITE;
                }

                if (mode.HasFlag(DcLocatorMode.SiteLookup))
                {
                    this.logger.LogTrace("Manual DCLocator: Finding site for server {server}, domain {domainDns} with flags {flags}", server, domainDns, flags.ToString());
                    string site = this.GetComputerSiteNameManual(domainDns, server);

                    if (site != null)
                    {
                        try
                        {
                            this.logger.LogTrace("Manual DCLocator: Attempting to find domain controller for site {site}, in domain {domainDns} with flags {flags}", site, domainDns, flags.ToString());

                            dc = NativeMethods.GetDomainControllerForDnsDomain(null, domainDns, site, flags);
                            this.logger.LogTrace("Manual DCLocator: Found DC {dc} for site {site} in domain {domainDns}, with flags {flags}", dc, site, domainDns, flags.ToString());
                            return dc;
                        }
                        catch (DirectoryException dex) when (dex.InnerException is Win32Exception wex && wex.NativeErrorCode == 1355)
                        {
                            mode &= ~DcLocatorMode.SiteLookup;
                            this.logger.LogWarning(dex, "There are no domain controllers in the site {site}", site);
                        }
                    }
                    else
                    {
                        this.logger.LogTrace("Manual DCLocator: No site found for server {server}", server);
                    }
                }
            }

            this.logger.LogTrace("Local DCLocator: Finding domain controller for domain {domainDns} with flags {flags}", domainDns, flags.ToString());
            dc = NativeMethods.GetDomainControllerForDnsDomain(null, domainDns, null, flags);
            this.logger.LogTrace("Local DCLocator: Found DC {dc} for domain {domainDns}, with flags {flags}", dc, domainDns, flags.ToString());
            return dc;
        }

        public bool DoesSchemaAttributeExist(string dnsDomain, string attributeName)
        {
            return this.GetSchemaAttributeGuid(dnsDomain, attributeName) != null;
        }

        public Guid? GetSchemaAttributeGuid(string dnsDomain, string attributeName)
        {
            string key = $"{dnsDomain}-{attributeName}";

            return attributeGuidCache.GetOrAdd(key, k =>
            {
                DirectorySearcher d = new DirectorySearcher
                {
                    SearchRoot = this.GetSchemaNamingContext(dnsDomain),
                    SearchScope = SearchScope.Subtree,
                    Filter = $"(&(objectClass=attributeSchema)(lDAPDisplayName={attributeName})(!(isDefunct=true)))"
                };

                d.PropertiesToLoad.Add("schemaIDGUID");

                SearchResultCollection result = d.FindAll();

                if (result.Count > 1)
                {
                    throw new InvalidOperationException($"More than one attribute called {attributeName} was found");
                }

                if (result.Count == 0)
                {
                    return null;
                }

                return result[0].GetPropertyGuid("schemaIDGUID");
            });
        }

        public Guid? GetSchemaObjectGuid(string dnsDomain, string objectName)
        {
            string key = $"{dnsDomain}-{objectName}";

            return objectGuidCache.GetOrAdd(key, k =>
            {
                DirectorySearcher d = new DirectorySearcher
                {
                    SearchRoot = this.GetSchemaNamingContext(dnsDomain),
                    SearchScope = SearchScope.Subtree,
                    Filter = $"(&(objectClass=classSchema)(lDAPDisplayName={objectName})(!(isDefunct=true)))"
                };

                d.PropertiesToLoad.Add("schemaIDGUID");

                SearchResultCollection result = d.FindAll();

                if (result.Count > 1)
                {
                    throw new InvalidOperationException($"More than one object called {objectName} was found");
                }

                if (result.Count == 0)
                {
                    return null;
                }

                return result[0].GetPropertyGuid("schemaIDGUID");
            });
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
