using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Internal;
using NLog;

namespace Lithnet.Laps.Web.Authorization
{
    public class JsonTargetAuthorizationService : IAuthorizationService
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly IList<IJsonTarget> targets;

        private readonly IAceEvaluator aceEvaluator;

        public JsonTargetAuthorizationService(IDirectory directory, ILogger logger, IJsonTargetsProvider provider, IAceEvaluator aceEvaluator)
        {
            this.directory = directory;
            this.logger = logger;
            this.targets = provider.Targets ?? new List<IJsonTarget>();
            this.aceEvaluator = aceEvaluator;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer)
        {
            var targets = this.GetMatchingTargetsForComputer(computer);

            if (targets.Count == 0)
            {
                this.logger.Trace($"User {user.SamAccountName} is denied access to the password for computer {computer.SamAccountName} because the computer did not match any of the configured targets");

                return new AuthorizationResponse()
                {
                    Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
                };
            }

            foreach (IJsonTarget j in targets)
            {
                foreach (var ace in j.Acl?.Where(t => t.Type == AceType.Deny))
                {
                    if (this.aceEvaluator.IsMatchingAce(ace, computer, user))
                    {
                        this.logger.Trace($"User {user.SamAccountName} matches deny ACE {ace.Sid ?? ace.Name} and is denied from reading passwords for computer {computer.SamAccountName} from target {j.Name}");

                        return new AuthorizationResponse()
                        {
                            MatchedRuleDescription = $"{j.Type}: {j.Name}",
                            MatchedPrincipal = ace.Sid ?? ace.Name,
                            Code = AuthorizationResponseCode.ExplicitlyDenied,
                            NotificationChannels = this.GetNotificationRecipients(j, ace, false),
                        };
                    }
                    else
                    {
                        this.logger.Trace($"Denied principal {ace.Sid ?? ace.Name} does not match current user {user.SamAccountName}");
                    }
                }
            }

            List<string> failureNotificationRecipients = new List<string>();

            foreach (IJsonTarget j in targets)
            {
                foreach (var ace in j.Acl?.Where(t => t.Type == AceType.Allow))
                {
                    if (this.aceEvaluator.IsMatchingAce(ace, computer, user))
                    {
                        this.logger.Trace($"User {user.SamAccountName} matches allow ACE {ace.Sid ?? ace.Name} and is authorized to read passwords for computer {computer.SamAccountName} from target {j.Name}");

                        return new AuthorizationResponse()
                        {
                            MatchedRuleDescription = $"{j.Type}: {j.Name}",
                            MatchedPrincipal = ace.Sid ?? ace.Name,
                            Code = AuthorizationResponseCode.Success,
                            NotificationChannels = this.GetNotificationRecipients(j, ace, true),
                            ExpireAfter = j.ExpireAfter,
                        };
                    }

                    this.logger.Trace($"Allowed principal {ace.Sid ?? ace.Name} does not match current user {user.SamAccountName}");
                    j.NotificationChannels?.OnFailure?.ForEach(u => failureNotificationRecipients.Add(u));
                }
            }

            this.logger.Trace($"User {user.SamAccountName} does not match any target access control lists and is denied access to the password for computer {computer.SamAccountName}");

            return new AuthorizationResponse()
            {
                Code = AuthorizationResponseCode.NoMatchingRuleForUser,
                NotificationChannels = failureNotificationRecipients
            };
        }

        private IList<string> GetNotificationRecipients(IJsonTarget t, IAce a, bool success)
        {
            List<string> list = new List<string>();

            if (success)
            {
                t.NotificationChannels?.OnSuccess?.ForEach(u => list.Add(u));
                a.NotificationChannels?.OnSuccess?.ForEach(u => list.Add(u));
            }
            else
            {
                t.NotificationChannels?.OnFailure?.ForEach(u => list.Add(u));
                a.NotificationChannels?.OnFailure?.ForEach(u => list.Add(u));
            }

            return list;
        }


        private IList<IJsonTarget> GetMatchingTargetsForComputer(IComputer computer)
        {
            List<IJsonTarget> matchingTargets = new List<IJsonTarget>();

            foreach (var target in this.targets.OrderBy(t => (int)t.Type))
            {
                try
                {
                    if (target.Type == TargetType.Container)
                    {
                        if (this.directory.IsComputerInOu(computer, target.Name))
                        {
                            this.logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.Computer)
                    {
                        IComputer p;
                        try
                        {
                            p = this.directory.GetComputer(target.Name);
                        }
                        catch (NotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target computer {target.Name} was not found in the directory");
                            continue;
                        }

                        if (p.Sid == computer.Sid)
                        {
                            this.logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        IGroup g;
                        try
                        {
                            g = this.directory.GetGroup(target.Name);
                        }
                        catch (NotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target group {target.Name} was not found in the directory");
                            continue;
                        }

                        if (this.directory.IsSidInPrincipalToken(computer.Sid, computer, g.Sid))
                        {
                            this.logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                            matchingTargets.Add(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogEventError(EventIDs.TargetRuleProcessingError, $"An error occurred processing the target {target.Type}:{target.Name}", ex);
                }
            }

            return matchingTargets;
        }
    }
}