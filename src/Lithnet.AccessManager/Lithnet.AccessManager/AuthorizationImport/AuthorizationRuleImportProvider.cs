using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class AuthorizationRuleImportProvider : IAuthorizationRuleImportProvider
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IComputerPrincipalProviderCsv csvProvider;
        private readonly IComputerPrincipalProviderRpc rpcProvider;
        private readonly IComputerPrincipalProviderMsLaps msLapsProvider;
        private readonly IComputerPrincipalProviderBitLocker bitLockerProvider;
        private ConcurrentDictionary<SecurityIdentifier, string> nameCache;
        public event EventHandler<ProcessingComputerArgs> OnStartProcessingComputer;

        public AuthorizationRuleImportProvider(ILogger<AuthorizationRuleImportProvider> logger, IDirectory directory, IComputerPrincipalProviderCsv csvProvider, IComputerPrincipalProviderRpc rpcProvider, IComputerPrincipalProviderMsLaps msLapsProvider, IComputerPrincipalProviderBitLocker bitLockerProvider)
        {
            this.logger = logger;
            this.directory = directory;
            this.rpcProvider = rpcProvider;
            this.csvProvider = csvProvider;
            this.msLapsProvider = msLapsProvider;
            this.bitLockerProvider = bitLockerProvider;
        }

        public AuthorizationRuleImportResults BuildPrincipalMap(AuthorizationRuleImportSettings settings)
        {
            IComputerPrincipalProvider provider;

            switch (settings.DiscoveryMode)
            {
                case UserDiscoveryMode.File:
                    csvProvider.ImportPrincipalMappings(settings.ImportFile, settings.HasHeaderRow);
                    provider = csvProvider;
                    break;

                case UserDiscoveryMode.LocalAdminRpc:
                    provider = rpcProvider;
                    break;

                case UserDiscoveryMode.Laps:
                    provider = msLapsProvider;
                    break;

                case UserDiscoveryMode.BitLocker:
                    provider = bitLockerProvider;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return this.BuildPrincipalMap(provider, settings);
        }


        public int GetComputerCount(string startingOU)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{startingOU}"),
                SearchScope = SearchScope.Subtree,
                Filter = "(&(objectCategory=computer))",
                PropertyNamesOnly = true,
                PageSize = 1000
            };

            d.PropertiesToLoad.AddRange(new[] { "distinguishedName" });
            SearchResultCollection resultCollection = d.FindAll();

            return resultCollection.Count;
        }

        private AuthorizationRuleImportResults BuildPrincipalMap(IComputerPrincipalProvider provider, AuthorizationRuleImportSettings settings)
        {
            OUPrincipalMapping ou = new OUPrincipalMapping($"LDAP://{settings.ImportOU}", settings.ImportOU);

            AuthorizationRuleImportResults results = new AuthorizationRuleImportResults
            {
                MappedOU = ou,
                ComputerErrors = new List<ComputerPrincipalMapping>()
            };

            nameCache = new ConcurrentDictionary<SecurityIdentifier, string>();

            this.BuildPrincipalMap(ou, provider, settings, results.ComputerErrors);

            nameCache.Clear();

            return results;
        }

        private IEnumerable<SearchResult> GetComputersFromOU(string adsPath, bool includeSubTree, IComputerPrincipalProvider provider)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry(adsPath),
                SearchScope = includeSubTree ? SearchScope.Subtree : SearchScope.OneLevel,
                Filter = "(&(objectCategory=computer))",
                PageSize = 1000,
                SecurityMasks = SecurityMasks.Dacl | SecurityMasks.Group | SecurityMasks.Owner
            };

            HashSet<string> properties = new HashSet<string>(provider.ComputerPropertiesToGet, StringComparer.OrdinalIgnoreCase)
            {
                "distinguishedName",
                "samAccountName",
                "cn",
                "msDS-PrincipalName",
                "objectSid"
            };

            d.PropertiesToLoad.AddRange(properties.ToArray());

            SearchResultCollection resultCollection = d.FindAll();

            foreach (SearchResult item in resultCollection)
            {
                yield return item;
            }
        }

        private IEnumerable<SearchResult> GetOUs(string adsPath, bool subTree)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry(adsPath),
                SearchScope = subTree ? SearchScope.Subtree : SearchScope.OneLevel,
                Filter = "(|(objectCategory=organizationalUnit)(objectCategory=container))",
                PageSize = 1000
            };

            d.PropertiesToLoad.Add("distinguishedName");
            SearchResultCollection resultCollection = d.FindAll();

            foreach (SearchResult item in resultCollection)
            {
                yield return item;
            }
        }

        private void BuildPrincipalMap(OUPrincipalMapping ou, IComputerPrincipalProvider principalProvider, AuthorizationRuleImportSettings settings, List<ComputerPrincipalMapping> errorComputers)
        {
            settings.CancellationToken.ThrowIfCancellationRequested();

            foreach (SearchResult computer in this.GetComputersFromOU(ou.AdsPath, false, principalProvider))
            {
                settings.CancellationToken.ThrowIfCancellationRequested();

                this.OnStartProcessingComputer?.Invoke(this, new ProcessingComputerArgs(computer.GetPropertyString("samAccountName")));

                if (settings.ComputerFilters.Any(t => t.IsMatch(computer.GetPropertyString("samAccountName"))))
                {
                    continue;
                }

                this.logger.LogTrace("Found computer {computer} in ou {ou}", computer.GetPropertyString("samAccountName"), ou.OUName);

                ComputerPrincipalMapping ce = new ComputerPrincipalMapping(computer, ou);

                ou.Computers.Add(ce);

                try
                {
                    List<SecurityIdentifier> admins = principalProvider.GetPrincipalsForComputer(computer, settings.FilterLocalAccounts);

                    this.logger.LogTrace("Got {number} local admins from computer {computer}", admins.Count, computer.GetPropertyString("samAccountName"));

                    foreach (var admin in admins)
                    {
                        settings.CancellationToken.ThrowIfCancellationRequested();

                        if (this.ShouldFilter(admin, settings))
                        {
                            this.logger.LogTrace("Filtering admin {admin}", admin.ToString());
                        }
                        else
                        {
                            ce.Principals.Add(admin);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, "Failed to get admins from computer {computer}", computer.GetPropertyString("samAccountName"));
                    ce.HasError = true;
                    ce.IsMissing = ex is ObjectNotFoundException;
                    ce.Exception = ex;
                    errorComputers.Add(ce);
                }
            }

            foreach (var childOU in this.GetOUs(ou.AdsPath, false))
            {
                settings.CancellationToken.ThrowIfCancellationRequested();

                this.logger.LogTrace("Found ou {ou}", childOU.GetPropertyString("distinguishedName"));

                OUPrincipalMapping childou = new OUPrincipalMapping(childOU, ou);

                ou.DescendantOUs.Add(childou);
                this.BuildPrincipalMap(childou, principalProvider, settings, errorComputers);
            }
        }

        private bool ShouldFilter(SecurityIdentifier sid, AuthorizationRuleImportSettings settings)
        {
            string name = null;

            if (!sid.IsAccountSid())
            {
                return true;
            }

            if (settings.PrincipalSidFilter.Contains(sid))
            {
                return true;
            }

            if (settings.FilterUnresolvablePrincipals == false && settings.PrincipalFilters.Count == 0)
            {
                return false;
            }

            try
            {
                name = nameCache.GetOrAdd(sid, (value) => this.directory.TranslateName(sid.ToString(), Interop.DsNameFormat.SecurityIdentifier, Interop.DsNameFormat.Nt4Name));
            }
            catch
            {
                if (settings.FilterUnresolvablePrincipals)
                {
                    return true;
                }
            }


            return settings.PrincipalFilters.Any(t => t.IsMatch(sid.ToString()) || (name != null && t.IsMatch(name)));
        }

        public void WriteReport(AuthorizationRuleImportResults results, string path)
        {
            string errorPath = $"{Path.GetDirectoryName(path)}\\{Path.GetFileNameWithoutExtension(path)}-errors{Path.GetExtension(path)}";
            using (var errorWriter = new StreamWriter(errorPath, false))
            {
                using (var writer = new StreamWriter(path, false))
                {
                    writer.AutoFlush = true;

                    this.WriteReport(results.MappedOU, writer, errorWriter);
                    writer.Flush();
                }
            }
        }

        private void WriteReport(OUPrincipalMapping entry, StreamWriter dataWriter, StreamWriter errorWriter)
        {
            var admins = entry.UniquePrincipals;
            var computers = entry.Computers;

            if (admins.Count > 0 || computers.Count > 0)
            {
                if (admins.Count == 0 && computers.All(t => t.UniquePrincipals.Count == 0))
                {
                    return;
                }

                dataWriter.WriteLine("---------------------------");
                dataWriter.WriteLine($"OU: {entry.OUName}");
                //dataWriter.WriteLine($"Non-inherited admins: {admins.Count}");

                foreach (var item in admins)
                {
                    try
                    {
                        NTAccount account = (NTAccount)item.Translate(typeof(NTAccount));
                        dataWriter.WriteLine("\t" + account);
                    }
                    catch
                    {
                        dataWriter.WriteLine("\t" + item);
                    }
                }

                foreach (var computer in computers)
                {
                    var compAdmins = computer.UniquePrincipals;

                    if (computer.HasError)
                    {
                        errorWriter.WriteLine($"Computer: {computer.PrincipalName}");
                        errorWriter.WriteLine($"\tError: {computer.Exception}");
                        errorWriter.WriteLine("--------------------------------------------------------------------");
                        continue;
                    }

                    if (computer.UniquePrincipals.Count == 0)
                    {
                        continue;
                    }

                    dataWriter.WriteLine($"Computer: {computer.PrincipalName}");
                    //dataWriter.WriteLine($"\t\tAdmins for computer: {computer.Admins.Count}");
                    //dataWriter.WriteLine($"\tNon-inherited admins for computer: {compAdmins.Count}");

                    foreach (var compAdmin in compAdmins)
                    {
                        try
                        {
                            NTAccount account = (NTAccount)compAdmin.Translate(typeof(NTAccount));
                            dataWriter.WriteLine("\t" + account.ToString());
                        }
                        catch
                        {
                            dataWriter.WriteLine("\t" + compAdmin.ToString());
                        }
                    }

                    if (compAdmins.Count > 0)
                    {
                        // dataWriter.WriteLine("****");
                    }
                }
            }

            foreach (var childOU in entry.DescendantOUs)
            {
                this.WriteReport(childOU, dataWriter, errorWriter);
            }
        }
    }
}
