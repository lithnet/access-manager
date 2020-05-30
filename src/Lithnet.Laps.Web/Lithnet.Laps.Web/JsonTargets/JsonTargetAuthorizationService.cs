using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lithnet.Laps.Web.Models;
using NLog;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class JsonTargetAuthorizationService : IAuthorizationService
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly JsonTarget[] targets;

        public JsonTargetAuthorizationService(IDirectory directory, ILogger logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer)
        {
            var targets = this.GetMatchingTargetsForComputer(computer);

            if (targets.Count == 0)
            {
                return new AuthorizationResponse()
                {
                    ResponseCode = AuthorizationResponseCode.NoMatchingRuleForComputer,
                };
            }

            foreach (JsonTarget j in targets)
            {
                foreach (var ace in j.Acl.Where(t => t.Type == AceType.Deny))
                {
                    ISecurityPrincipal principal = this.directory.GetPrincipal(ace.Sid ?? ace.Name);

                    this.logger.Trace($"Reader principal {ace.Sid ?? ace.Name} found in directory as user {principal.DistinguishedName}");

                    if (this.directory.IsSidInPrincipalToken(computer.Sid.AccountDomainSid, user, principal.Sid))
                    {
                        this.logger.Trace($"User {user.SamAccountName} matches reader principal {ace.Sid ?? ace.Name} denied fromreading passwords from target {j.Name}");

                        return new AuthorizationResponse()
                        {
                            MatchedAceID = j.Name,
                            ResponseCode = AuthorizationResponseCode.UserDeniedByAce,
                            NotificationRecipients = j.EmailAuditing.FailureRecipients
                        };
                    };
                }
            }

            foreach (JsonTarget j in targets)
            {
                foreach (var ace in j.Acl.Where(t => t.Type == AceType.Allow))
                {
                    ISecurityPrincipal principal = this.directory.GetPrincipal(ace.Sid ?? ace.Name);

                    this.logger.Trace($"Reader principal {ace.Sid ?? ace.Name} found in directory as user {principal.DistinguishedName}");

                    if (this.directory.IsSidInPrincipalToken(computer.Sid.AccountDomainSid, user, principal.Sid))
                    {
                        this.logger.Trace($"User {user.SamAccountName} matches ACE {ace.Sid ?? ace.Name} is authorized to read passwords from target {j.Name}");

                        return new AuthorizationResponse()
                        {
                            MatchedAceID = j.Name,
                            ExpireAfter = j.ExpireAfter,
                            NotificationRecipients = j.EmailAuditing.SuccessRecipients,
                            ResponseCode = AuthorizationResponseCode.Success
                        };
                    };
                }
            }

            return new AuthorizationResponse()
            {
                ResponseCode = AuthorizationResponseCode.NoMatchingRuleForUser
            };
        }

        private IList<JsonTarget> GetMatchingTargetsForComputer(IComputer computer)
        {
            List<JsonTarget> matchingTargets = new List<JsonTarget>();

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
                    this.logger.Error(ex, $"An error occurred processing the target {target.Type}:{target.Name}");
                }
            }

            return matchingTargets;
        }
    }
}