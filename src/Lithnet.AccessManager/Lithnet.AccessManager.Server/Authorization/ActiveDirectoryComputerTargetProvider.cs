using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class ActiveDirectoryComputerTargetProvider : IComputerTargetProvider
    {
        private readonly IDirectory directory;
        private readonly ITargetDataProvider targetDataProvider;
        private readonly ILogger logger;

        public ActiveDirectoryComputerTargetProvider(IDirectory directory, ITargetDataProvider targetDataProvider, ILogger<ActiveDirectoryComputerTargetProvider> logger)
        {
            this.logger = logger;
            this.targetDataProvider = targetDataProvider;
            this.directory = directory;
        }
        public bool CanProcess(IComputer computer)
        {
            return computer is IActiveDirectoryComputer || (computer is IDevice d && d.AuthorityType == AuthorityType.ActiveDirectory);
        }

        public Task<IList<SecurityDescriptorTarget>> GetMatchingTargetsForComputer(IComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
        {
            if (!(computer is IActiveDirectoryComputer adComputer))
            {
                if (computer is IDevice d && d.AuthorityType == AuthorityType.ActiveDirectory)
                {
                    adComputer = this.directory.GetComputer(d.SecurityIdentifier);
                    return Task.FromResult(this.GetMatchingTargetsForComputer(adComputer, targets));
                }

                throw new InvalidOperationException("The object passed to the method was of an incorrect type");
            }

            return Task.FromResult(this.GetMatchingTargetsForComputer(adComputer, targets));
        }

        public IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IActiveDirectoryComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
        {
            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();

            Lazy<List<SecurityIdentifier>> computerTokenSids = new Lazy<List<SecurityIdentifier>>(() => this.directory.GetTokenGroups(computer, computer.Sid.AccountDomainSid).ToList());
            Lazy<List<Guid>> computerParents = new Lazy<List<Guid>>(() => computer.GetParentGuids().ToList());

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
                        case TargetType.Container:
                            {
                                if (computerParents.Value.Any(t => t == targetData.ContainerGuid))
                                {
                                    this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target OU {target.Target}");
                                    matchingTargets.Add(target);
                                }

                                break;
                            }

                        case TargetType.Computer:
                            {
                                if (targetData.Sid == computer.SecurityIdentifier)
                                {
                                    this.logger.LogTrace($"Matched {computer.FullyQualifiedName} to target {target.Id}");
                                    matchingTargets.Add(target);
                                }

                                break;
                            }

                        case TargetType.Group:
                            {
                                if (this.directory.IsSidInPrincipalToken(targetData.Sid, computerTokenSids.Value))
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
