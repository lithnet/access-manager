using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class ComputerTargetProviderAd : IComputerTargetProvider
    {
        private readonly IActiveDirectory directory;
        private readonly ITargetDataProvider targetDataProvider;
        private readonly ILogger logger;
        private readonly IComputerTokenSidProvider computerTokenSidProvider;

        public ComputerTargetProviderAd(IActiveDirectory directory, ITargetDataProvider targetDataProvider, ILogger<ComputerTargetProviderAd> logger, IComputerTokenSidProvider computerTokenSidProvider)
        {
            this.logger = logger;
            this.computerTokenSidProvider = computerTokenSidProvider;
            this.targetDataProvider = targetDataProvider;
            this.directory = directory;
        }
        public bool CanProcess(IComputer computer)
        {
            return computer is IActiveDirectoryComputer || (computer is IDevice d && d.AuthorityType == AuthorityType.ActiveDirectory);
        }

        public async Task<IList<SecurityDescriptorTarget>> GetMatchingTargetsForComputer(IComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
        {
            if (!(computer is IActiveDirectoryComputer adComputer))
            {
                if (computer is IDevice d && d.AuthorityType == AuthorityType.ActiveDirectory)
                {
                    adComputer = this.directory.GetComputer(d.SecurityIdentifier);
                    return await this.GetMatchingTargetsForComputer(adComputer, targets);
                }

                throw new InvalidOperationException("The object passed to the method was of an incorrect type");
            }

            return await this.GetMatchingTargetsForComputer(adComputer, targets);
        }

        public async Task<IList<SecurityDescriptorTarget>> GetMatchingTargetsForComputer(IActiveDirectoryComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
        {
            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();
            List<SecurityIdentifier> computerTokenSids = null;
            List<Guid> computerParents = null;

            foreach (var target in targets.OrderBy(t => (int)t.Type).ThenByDescending(this.targetDataProvider.GetSortOrder))
            {
                TargetData targetData = this.targetDataProvider.GetTargetData(target);

                try
                {
                    if (target.IsInactive())
                    {
                        continue;
                    }

                    switch (target.Type)
                    {
                        case TargetType.AdContainer:
                            {
                                computerParents ??= computer.GetParentGuids().ToList();

                                if (computerParents.Any(t => t == targetData.ContainerGuid))
                                {
                                    this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target OU {target.Target}");
                                    matchingTargets.Add(target);
                                }

                                break;
                            }

                        case TargetType.AdComputer:
                            {
                                if (targetData.Sid == computer.SecurityIdentifier)
                                {
                                    this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target {target.Id}");
                                    matchingTargets.Add(target);
                                }

                                break;
                            }

                        case TargetType.AdGroup:
                            {
                                computerTokenSids ??= await this.computerTokenSidProvider.GetTokenSids(computer);

                                if (this.directory.IsSidInPrincipalToken(targetData.Sid, computerTokenSids))
                                {
                                    this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target {target.Id}");
                                    matchingTargets.Add(target);
                                }

                                break;
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
