using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public abstract class ImportProviderComputerDiscovery : IImportProvider
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;

        private ConcurrentDictionary<SecurityIdentifier, ISecurityPrincipal> principalCache;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessStart;

        public event EventHandler<ImportProcessingEventArgs> OnItemProcessFinish;

        private readonly ImportSettingsComputerDiscovery settings;

        protected ImportProviderComputerDiscovery(ImportSettingsComputerDiscovery settings, ILogger<ImportProviderComputerDiscovery> logger, IDirectory directory)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
        }

        private List<SecurityDescriptorTarget> ConvertToTargets(OUPrincipalMapping entry)
        {
            List<SecurityDescriptorTarget> targets = new List<SecurityDescriptorTarget>();

            this.PopulateTargets(entry, targets);

            return targets;
        }

        private void PopulateTargets(OUPrincipalMapping entry, List<SecurityDescriptorTarget> targets)
        {
            this.logger.LogTrace("Processing OU {ou}", entry.AdsPath);

            bool doNotConsolidate = settings.DoNotConsolidate || (settings.DoNotConsolidateOnError && entry.HasDescendantsWithErrors);

            if (!doNotConsolidate)
            {
                if (entry.UniquePrincipals.Count > 0)
                {
                    targets.Add(this.ConvertToTarget(entry));
                }
            }

            foreach (var computer in entry.Computers)
            {
                var admins = doNotConsolidate ? computer.Principals : computer.UniquePrincipals;

                if (!computer.HasError && admins.Count > 0)
                {
                    targets.Add(this.ConvertToTarget(computer, admins));
                }
            }

            foreach (var ou in entry.DescendantOUs)
            {
                this.PopulateTargets(ou, targets);
            }
        }

        private SecurityDescriptorTarget ConvertToTarget(ComputerPrincipalMapping computer, HashSet<SecurityIdentifier> admins)
        {
            this.logger.LogTrace("Converting computer {computer} to target with {admins} admins", computer.PrincipalName, admins.Count);

            SecurityDescriptorTarget target = new SecurityDescriptorTarget()
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Description = settings.RuleDescription.Replace("{targetName}", computer.PrincipalName, StringComparison.OrdinalIgnoreCase),
                Target = computer.Sid.ToString(),
                Type = TargetType.Computer,
                Id = Guid.NewGuid().ToString(),
                Notifications = settings.Notifications,
                Jit = new SecurityDescriptorTargetJitDetails()
                {
                    AuthorizingGroup = settings.JitAuthorizingGroup,
                    ExpireAfter = settings.JitExpireAfter
                },
                Laps = new SecurityDescriptorTargetLapsDetails()
                {
                    ExpireAfter = settings.LapsExpireAfter
                }
            };

            AccessMask mask = 0;
            mask |= settings.AllowLaps ? AccessMask.LocalAdminPassword : 0;
            mask |= settings.AllowJit ? AccessMask.Jit : 0;
            mask |= settings.AllowLapsHistory ? AccessMask.LocalAdminPasswordHistory : 0;
            mask |= settings.AllowBitLocker ? AccessMask.BitLocker : 0;

            DiscretionaryAcl acl = new DiscretionaryAcl(false, false, admins.Count);

            foreach (var sid in admins)
            {
                acl.AddAccess(AccessControlType.Allow, sid, (int)mask, InheritanceFlags.None, PropagationFlags.None);
            }

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, acl);

            target.SecurityDescriptor = sd.GetSddlForm(AccessControlSections.All);

            return target;
        }

        private SecurityDescriptorTarget ConvertToTarget(OUPrincipalMapping entry)
        {
            this.logger.LogTrace("Converting OU {ou} to target with {admins} admins", entry.AdsPath, entry.UniquePrincipals.Count);

            SecurityDescriptorTarget target = new SecurityDescriptorTarget()
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Description = settings.RuleDescription.Replace("{targetName}", entry.OUName, StringComparison.OrdinalIgnoreCase),
                Target = entry.OUName,
                Type = TargetType.Container,
                Id = Guid.NewGuid().ToString(),
                Notifications = settings.Notifications,
                Jit = new SecurityDescriptorTargetJitDetails()
                {
                    AuthorizingGroup = settings.JitAuthorizingGroup,
                    ExpireAfter = settings.JitExpireAfter
                },
                Laps = new SecurityDescriptorTargetLapsDetails()
                {
                    ExpireAfter = settings.LapsExpireAfter
                }
            };

            AccessMask mask = 0;
            mask |= settings.AllowLaps ? AccessMask.LocalAdminPassword : 0;
            mask |= settings.AllowJit ? AccessMask.Jit : 0;
            mask |= settings.AllowLapsHistory ? AccessMask.LocalAdminPasswordHistory : 0;
            mask |= settings.AllowBitLocker ? AccessMask.BitLocker : 0;

            DiscretionaryAcl acl = new DiscretionaryAcl(false, false, entry.UniquePrincipals.Count);

            foreach (var sid in entry.UniquePrincipals)
            {
                acl.AddAccess(AccessControlType.Allow, sid, (int)mask, InheritanceFlags.None, PropagationFlags.None);
            }

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, acl);

            target.SecurityDescriptor = sd.GetSddlForm(AccessControlSections.All);

            return target;
        }

        public int GetEstimatedItemCount()
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{settings.ImportOU}"),
                SearchScope = SearchScope.Subtree,
                Filter = "(&(objectCategory=computer))",
                PropertyNamesOnly = true,
                PageSize = 1000
            };

            d.PropertiesToLoad.AddRange(new[] { "distinguishedName" });
            SearchResultCollection resultCollection = d.FindAll();

            return resultCollection.Count;
        }

        public abstract ImportResults Import();

        protected ImportResults PerformComputerDiscovery(IComputerPrincipalProvider provider)
        {
            OUPrincipalMapping ou = new OUPrincipalMapping($"LDAP://{settings.ImportOU}", settings.ImportOU);

            ImportResults results = new ImportResults
            {
                DiscoveryErrors = new List<DiscoveryError>(),
            };

            principalCache = new ConcurrentDictionary<SecurityIdentifier, ISecurityPrincipal>();

            this.PerformComputerDiscovery(ou, provider, results.DiscoveryErrors);

            principalCache.Clear();

            results.Targets = this.ConvertToTargets(ou);

            return results;
        }

        private IEnumerable<SearchResult> GetComputersFromOU(string adsPath, bool includeSubTree, IComputerPrincipalProvider provider)
        {
            string additionaFilter = string.Empty;

            if (this.settings.FilterDisabledComputers)
            {
                additionaFilter += "(!(userAccountControl:1.2.840.113556.1.4.803:=2))";
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry(adsPath),
                SearchScope = includeSubTree ? SearchScope.Subtree : SearchScope.OneLevel,
                Filter = $"(&(objectCategory=computer){additionaFilter})",
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

            if (this.settings.ExcludeConflictObjects)
            {
                properties.Add("CNF");
            }

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

        private void PerformComputerDiscovery(OUPrincipalMapping ou, IComputerPrincipalProvider principalProvider, List<DiscoveryError> discoveryErrors)
        {
            settings.CancellationToken.ThrowIfCancellationRequested();

            foreach (SearchResult computer in this.GetComputersFromOU(ou.AdsPath, false, principalProvider))
            {
                settings.CancellationToken.ThrowIfCancellationRequested();
                string computerName = computer.GetPropertyString("msDS-PrincipalName");
                try
                {
                    this.OnItemProcessStart?.Invoke(this, new ImportProcessingEventArgs(computerName));

                    if (this.ShouldFilterComputer(computer))
                    {
                        this.logger.LogTrace("Filtering computer {computer}", computerName);
                        continue;
                    }

                    this.logger.LogTrace("Found computer {computer} in ou {ou}", computerName, ou.OUName);

                    ComputerPrincipalMapping ce = new ComputerPrincipalMapping(computer, ou);

                    ou.Computers.Add(ce);

                    try
                    {
                        HashSet<SecurityIdentifier> principalsForComputer = new HashSet<SecurityIdentifier>(principalProvider.GetPrincipalsForComputer(computer, settings.FilterLocalAccounts));

                        this.logger.LogTrace("Got {number} principals from computer {computer}", principalsForComputer.Count, computerName);

                        foreach (var principal in principalsForComputer)
                        {
                            settings.CancellationToken.ThrowIfCancellationRequested();

                            if (this.ShouldFilter(principal, out DiscoveryError filterReason))
                            {
                                if (filterReason != null)
                                {
                                    filterReason.Target = computerName;
                                    ce.DiscoveryErrors.Add(filterReason);
                                    this.logger.LogTrace("Filtering principal {principal} with reason: {reason}", principal.ToString(), filterReason);
                                }
                            }
                            else
                            {
                                ce.Principals.Add(principal);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogTrace(ex, "Failed to get principals from computer {computer}", computerName);
                        ce.HasError = true;
                        ce.Exception = ex;
                        ce.IsMissing = ex is ObjectNotFoundException;
                        ce.DiscoveryErrors.Add(new DiscoveryError() { Target = computerName, Message = ex.Message, Type = DiscoveryErrorType.Error });
                    }

                    if (ce.DiscoveryErrors.Count > 0)
                    {
                        discoveryErrors.AddRange(ce.DiscoveryErrors);
                    }
                }
                finally
                {
                    this.OnItemProcessFinish?.Invoke(this, new ImportProcessingEventArgs(computerName));
                }
            }

            foreach (var childOU in this.GetOUs(ou.AdsPath, false))
            {
                settings.CancellationToken.ThrowIfCancellationRequested();

                this.logger.LogTrace("Found ou {ou}", childOU.GetPropertyString("distinguishedName"));

                OUPrincipalMapping childou = new OUPrincipalMapping(childOU, ou);

                ou.DescendantOUs.Add(childou);
                this.PerformComputerDiscovery(childou, principalProvider, discoveryErrors);
            }
        }

        private bool ShouldFilterComputer(SearchResult computer)
        {
            if (settings.ComputerFilter.Contains(computer.GetPropertySid("objectSid")))
            {
                return true;
            }

            if (settings.ExcludeConflictObjects && computer.GetPropertyGuid("CNF").HasValue)
            {
                return true;
            }

            return false;
        }

        private bool ShouldFilter(SecurityIdentifier sid, out DiscoveryError filteredReason)
        {
            if (!sid.IsAccountSid())
            {
                filteredReason = null;

                if (settings.FilterNonAccountSids)
                {
                    logger.LogInformation("Silently filtering non-account SID {sid}", sid);
                    //filteredReason = new DiscoveryError() { Message = "The principal is not an account SID", Principal = sid.ToString(), Type = DiscoveryErrorType.Warning };
                    return true;
                }
                else
                {
                    return false;
                }
            }

            ISecurityPrincipal principal = principalCache.GetOrAdd(sid, (value) =>
            {
                if (this.directory.TryGetPrincipal(sid, out ISecurityPrincipal p))
                {
                    return p;
                }
                else
                {
                    return null;
                }
            });

            if (settings.PrincipalFilter.Contains(sid))
            {
                filteredReason = new DiscoveryError() { Message = "The principal matched the import filter", Principal = principal?.MsDsPrincipalName ?? sid.ToString(), Type = DiscoveryErrorType.Informational };
                return true;
            }

            if (principal == null)
            {
                filteredReason = new DiscoveryError() { Message = "The principal was not found in the directory", Principal = sid.ToString(), Type = DiscoveryErrorType.Warning };
                return true;
            }

            if (!(principal is IUser || principal is IGroup))
            {
                filteredReason = new DiscoveryError() { Message = "The principal was not a user or group", Principal = principal.MsDsPrincipalName, Type = DiscoveryErrorType.Error };
                return true;
            }

            filteredReason = null;
            return false;
        }
    }
}
