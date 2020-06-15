using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.Laps.Web.ActionProviders;
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

        private readonly IJitAccessGroupResolver jitResolver;

        public JsonTargetAuthorizationService(IDirectory directory, ILogger logger, IJsonTargetsProvider provider, IAceEvaluator aceEvaluator, IJitAccessGroupResolver jitResolver)
        {
            this.directory = directory;
            this.logger = logger;
            this.targets = provider.Targets ?? new List<IJsonTarget>();
            this.aceEvaluator = aceEvaluator;
            this.jitResolver = jitResolver;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            var targets = this.GetMatchingTargetsForComputer(computer);

            if (targets.Count == 0)
            {
                this.logger.Trace($"User {user.MsDsPrincipalName} is denied access to the password for computer {computer.MsDsPrincipalName} because the computer did not match any of the configured targets");
                return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForComputer);
            }

            foreach (IJsonTarget j in targets)
            {
                foreach (var ace in j.Acl?.Where(t => t.Type == AceType.Deny))
                {
                    if (this.aceEvaluator.IsMatchingAce(ace, user, requestedAccess))
                    {
                        this.logger.Trace($"User {user.MsDsPrincipalName} matches deny ACE {ace.Sid ?? ace.Trustee} and is denied {requestedAccess} access for computer {computer.MsDsPrincipalName} from target {j.Name}");

                        return BuildAuthZResponseDenied(requestedAccess, j, ace);
                    }
                    else
                    {
                        this.logger.Trace($"Denied principal {ace.Sid ?? ace.Trustee} does not match current user {user.MsDsPrincipalName}");
                    }
                }
            }

            List<string> failureNotificationRecipients = new List<string>();

            foreach (IJsonTarget j in targets)
            {
                foreach (var ace in j.Acl?.Where(t => t.Type == AceType.Allow))
                {
                    if (this.aceEvaluator.IsMatchingAce(ace, user, requestedAccess))
                    {
                        this.logger.Trace($"User {user.MsDsPrincipalName} matches allow ACE {ace.Sid ?? ace.Trustee} and is authorized to read passwords for computer {computer.MsDsPrincipalName} from target {j.Name}");

                        return BuildAuthZResponseSuccess(requestedAccess, j, ace, computer);
                    }

                    this.logger.Trace($"Allowed principal {ace.Sid ?? ace.Trustee} does not match current user {user.MsDsPrincipalName}");
                    j.NotificationChannels?.OnFailure?.ForEach(u => failureNotificationRecipients.Add(u));
                }
            }

            this.logger.Trace($"User {user.MsDsPrincipalName} does not match any target access control lists and is denied {requestedAccess} access to the computer {computer.MsDsPrincipalName}");

            return BuildAuthZResponseFailed(requestedAccess, AuthorizationResponseCode.NoMatchingRuleForUser, failureNotificationRecipients);
        }

        private static AuthorizationResponse BuildAuthZResponseFailed(AccessMask requestedAccess, AuthorizationResponseCode code, List<string> failureNotificationRecipients = null)
        {
            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(requestedAccess);
            response.Code = code;
            response.NotificationChannels = failureNotificationRecipients;

            return response;
        }

        private AuthorizationResponse BuildAuthZResponseDenied(AccessMask requestedAccess, IJsonTarget j, IAce ace)
        {
            AuthorizationResponse response = AuthorizationResponse.CreateAuthorizationResponse(requestedAccess);

            response.MatchedRuleDescription = $"{j.Type}: {j.Name}";
            response.Trustee = ace.Sid ?? ace.Trustee;
            response.Code = AuthorizationResponseCode.ExplicitlyDenied;
            response.NotificationChannels = this.GetNotificationRecipients(j, ace, false);

            return response;
        }

        private AuthorizationResponse BuildAuthZResponseSuccess(AccessMask requestedAccess, IJsonTarget j, IAce ace, IComputer computer)
        {
            AuthorizationResponse response;

            if (requestedAccess == AccessMask.Laps)
            {
                response = new LapsAuthorizationResponse()
                {
                    ExpireAfter = j.Laps.ExpireAfter
                };
            }
            else
            {
                response = new JitAuthorizationResponse()
                {
                    ExpireAfter = j.Jit.ExpireAfter,
                    AuthorizingGroup = this.jitResolver.GetJitAccessGroup(computer, j).MsDsPrincipalName
                };
            }

            response.MatchedRuleDescription = $"{j.Type}: {j.Name}";
            response.Trustee = ace.Sid ?? ace.Trustee;
            response.Code = AuthorizationResponseCode.Success;
            response.NotificationChannels = this.GetNotificationRecipients(j, ace, true);
            return response;
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
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target OU {target.Name}");
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
                        catch (ObjectNotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target computer {target.Name} was not found in the directory");
                            continue;
                        }

                        if (p.Sid == computer.Sid)
                        {
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target computer {target.Name}");
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
                        catch (ObjectNotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target group {target.Name} was not found in the directory");
                            continue;
                        }

                        if (this.directory.IsSidInPrincipalToken(g.Sid, computer, computer.Sid))
                        {
                            this.logger.Trace($"Matched {computer.MsDsPrincipalName} to target group {target.Name}");
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