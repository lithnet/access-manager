using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AzureActiveDirectoryComputerTargetProvider : IComputerTargetProvider
    {
        private readonly ITargetDataProvider targetDataProvider;
        private readonly ILogger logger;
        private readonly IAadGraphApiProvider aadProvider;
        private readonly IAmsLicenseManager licenseManager;

        public AzureActiveDirectoryComputerTargetProvider(ITargetDataProvider targetDataProvider, ILogger<AzureActiveDirectoryComputerTargetProvider> logger, IAadGraphApiProvider aadProvider, IAmsLicenseManager licenseManager)
        {
            this.logger = logger;
            this.aadProvider = aadProvider;
            this.licenseManager = licenseManager;
            this.targetDataProvider = targetDataProvider;
        }

        public bool CanProcess(IComputer computer)
        {
            return computer is IDevice d && d.AuthorityType == AuthorityType.AzureActiveDirectory && this.licenseManager.IsFeatureEnabled(LicensedFeatures.AzureAdDeviceSupport);
        }

        public async Task<IList<SecurityDescriptorTarget>> GetMatchingTargetsForComputer(IComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
        {
            if (!(computer is IDevice d) || (d.AuthorityType != AuthorityType.AzureActiveDirectory))
            {
                throw new InvalidOperationException("The object passed to the method was of an incorrect type");
            }

            this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.AzureAdDeviceSupport);

            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();

            List<SecurityIdentifier> computerTokenSids = null;

            foreach (var target in targets.OrderBy(t => (int)t.Type).ThenByDescending(this.targetDataProvider.GetSortOrder))
            {
                TargetData targetData = this.targetDataProvider.GetTargetData(target);

                try
                {
                    if (target.IsInactive())
                    {
                        continue;
                    }

                    if (target.Type == TargetType.AadComputer)
                    {
                        if (targetData.Sid == d.SecurityIdentifier)
                        {
                            this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.AadGroup)
                    {
                        if (computerTokenSids == null)
                        {
                            computerTokenSids = await this.aadProvider.GetDeviceGroupSids(computer.AuthorityId, computer.AuthorityDeviceId);
                        }

                        if (computerTokenSids.Any(t => t == targetData.Sid))
                        {
                            this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.TargetRuleProcessingError, ex, $"An error occurred processing the target {target.Id}:{target.Type}:{target.Target}");
                }
            }

            return matchingTargets;
        }
    }
}