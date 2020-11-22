using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class ComputerTargetProvider : IComputerTargetProvider
    {
        private readonly IDirectory directory;
        private readonly ITargetDataProvider targetDataProvider;
        private readonly ILogger logger;

        public ComputerTargetProvider(IDirectory directory, ITargetDataProvider targetDataProvider, ILogger<ComputerTargetProvider> logger)
        {
            this.logger = logger;
            this.targetDataProvider = targetDataProvider;
            this.directory = directory;
        }

        public IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IComputer computer, IEnumerable<SecurityDescriptorTarget> targets)
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

                    if (target.Type == TargetType.Container)
                    {
                        if (computerParents.Value.Any(t => t == targetData.ContainerGuid))
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target OU {target.Target}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.Computer)
                    {
                        if (targetData.Sid == computer.Sid)
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        if (this.directory.IsSidInPrincipalToken(targetData.Sid, computerTokenSids.Value))
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
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
