using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Providers
{
    public class JitAuthorizationRuleImportProvider
    {
        private readonly ILocalSam localSam;
        private readonly ILogger logger;
        private readonly IDirectory directory;

        public JitAuthorizationRuleImportProvider(ILogger<JitAuthorizationRuleImportProvider> logger, ILocalSam localSam, IDirectory directory)
        {
            this.localSam = localSam;
            this.logger = logger;
            this.directory = directory;
        }

        public IEnumerable<SecurityIdentifier> GetLocalAdmins(string computerDnsName, bool filterLocalAccounts, bool filterUnresolvablePrincipals)
        {
            SecurityIdentifier localMachineSid = null;

            try
            {
                if (filterLocalAccounts)
                {
                    localMachineSid = localSam.GetLocalMachineAuthoritySid(computerDnsName);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Unable to connect to get SID from remote computer {computer}", computerDnsName);
            }

            IList<SecurityIdentifier> members = this.localSam.GetLocalGroupMembers(computerDnsName, this.localSam.GetBuiltInAdministratorsGroupNameOrDefault(computerDnsName));

            foreach (var member in members)
            {
                if (filterLocalAccounts)
                {
                    if (localMachineSid != null)
                    {
                        if (member.IsEqualDomainSid(localMachineSid))
                        {
                            continue;
                        }
                    }

                    if (member.IsWellKnown(WellKnownSidType.AccountAdministratorSid))
                    {
                        continue;
                    }
                }

                try
                {
                    if (filterUnresolvablePrincipals && !directory.TryGetPrincipal(member, out _))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, "Unable to find principal {principal} in the directory", member);
                }

                yield return member;
            }
        }

        public IEnumerable<SearchResult> GetComputersFromOU(string adsPath, bool includeSubTree)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry(adsPath),
                SearchScope = includeSubTree ? SearchScope.Subtree : SearchScope.OneLevel,
                Filter = "(&(objectCategory=computer)(!(cn=test-*)))",
                PageSize = 1000
            };

            d.PropertiesToLoad.AddRange(new[] { "distinguishedName", "dnsHostName", "samAccountName", "cn" });
            SearchResultCollection resultCollection = d.FindAll();

            foreach (SearchResult item in resultCollection)
            {
                yield return item;
            }
        }

        public IEnumerable<SearchResult> GetOUs(string adsPath, bool subTree)
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

        public void GetOUEntries(OUEntry entry)
        {
            entry.ChildOUs = new List<OUEntry>();
            entry.Computers = new List<ComputerEntry>();

            foreach (SearchResult computer in this.GetComputersFromOU(entry.AdsPath, false))
            {
                this.logger.LogTrace("Found computer {computer} in ou {ou}", computer.GetPropertyString("samAccountName"), entry.OUName);

                ComputerEntry ce = new ComputerEntry()
                {
                    Computer = computer,
                    Parent = entry,
                    Admins = new List<SecurityIdentifier>()
                };

                entry.Computers.Add(ce);

                try
                {
                    var admins = this.GetLocalAdmins(computer.GetPropertyString("dnsHostName") ?? computer.GetPropertyString("cn"), true, true);
                    ce.Admins = admins.ToList();
                    this.logger.LogTrace("Got {number} local admins from computer {computer}", ce.Admins.ToList(), computer.GetPropertyString("samAccountName"));
                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, "Failed to get admins from computer {computer}", computer.GetPropertyString("samAccountName"));
                    ce.HasError = true;
                    ce.Exception = ex;
                }
            }

            foreach (var ou in this.GetOUs(entry.AdsPath, false))
            {
                this.logger.LogTrace("Found ou {ou}", ou.GetPropertyString("distinguishedName"));

                OUEntry childou = new OUEntry()
                {
                    OU = ou,
                    AdsPath = ou.Path,
                    OUName = ou.GetPropertyString("distinguishedName"),
                    Parent = entry,
                    ChildOUs = new List<OUEntry>(),
                    Computers = new List<ComputerEntry>(),
                };

                entry.ChildOUs.Add(childou);
                this.GetOUEntries(childou);
            }
        }
    }

    public class OUEntry
    {
        public List<ComputerEntry> Computers { get; set; }

        public List<OUEntry> ChildOUs { get; set; }

        public string OUName { get; set; }

        public string AdsPath { get; set; }

        public SearchResult OU { get; set; }

        public OUEntry Parent { get; set; }

        public IEnumerable<SecurityIdentifier> CommonAdmins
        {
            get
            {
                return ChildAdmins.FirstOrDefault()?.Distinct().Where(option => ChildAdmins.Skip(1).All(l => l.Contains(option)));
            }
        }

        public IEnumerable<IEnumerable<SecurityIdentifier>> ChildAdmins
        {
            get
            {
                foreach (var computer in this.Computers)
                {
                    if (!computer.HasError)
                    {
                        yield return computer.Admins;
                    }
                }

                foreach (var ou in this.ChildOUs)
                {
                    foreach (var list in ou.ChildAdmins)
                    {
                        yield return list;
                    }
                }
            }
        }
    }

    public class ComputerEntry
    {
        public SearchResult Computer { get; set; }

        public OUEntry Parent { get; set; }

        public bool HasError { get; set; }

        public Exception Exception { get; set; }

        public List<SecurityIdentifier> Admins { get; set; }
    }
}
