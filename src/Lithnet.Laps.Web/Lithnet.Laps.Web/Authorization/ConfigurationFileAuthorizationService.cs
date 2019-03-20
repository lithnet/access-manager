using NLog;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly LapsConfigSection configSection;
        private readonly ILogger logger;
        private readonly ActiveDirectory.ActiveDirectory activeDirectory;

        public ConfigurationFileAuthorizationService(LapsConfigSection configSection, ILogger logger,
            ActiveDirectory.ActiveDirectory activeDirectory)
        {
            this.configSection = configSection;
            this.logger = logger;
            this.activeDirectory = activeDirectory;
        }

        public AuthorizationResponse CanAccessPassword(UserPrincipal user, IComputer computer)
        {
            var computerPrincipal = activeDirectory.GetComputerPrincipal(computer.SamAccountName);
            var target = GetMatchingTargetOrNull(computerPrincipal);

            if (target == null)
            {
                return AuthorizationResponse.NoTarget(new UsersToNotify());
            }

            foreach (ReaderElement reader in target.Readers.OfType<ReaderElement>())
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.Name}");

                    return AuthorizationResponse.Authorized(((ITarget)target).UsersToNotify, target);
                }
            }

            return AuthorizationResponse.NoReader(((ITarget) target).UsersToNotify, target);
        }

        private bool IsReaderAuthorized(ReaderElement reader, UserPrincipal currentUser)
        {
            var readerPrincipal = activeDirectory.GetPrincipal(reader.Principal);

            if (currentUser.Equals(readerPrincipal))
            {
                return true;
            }

            if (readerPrincipal is GroupPrincipal group)
            {
                if (activeDirectory.IsPrincipalInGroup(currentUser, group))
                {
                    return true;
                }
            }

            return false;
        }

        private TargetElement GetMatchingTargetOrNull(ComputerPrincipal computer)
        {
            List<TargetElement> matchingTargets = new List<TargetElement>();

            foreach (TargetElement target in configSection.Configuration.Targets.OfType<TargetElement>().OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group))
            {
                if (target.Type == TargetType.Container)
                {
                    if (activeDirectory.IsPrincipalInOu(computer, target.Name))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                        matchingTargets.Add(target);
                    }

                    continue;
                }
                else if (target.Type == TargetType.Computer)
                {
                    ComputerPrincipal p = activeDirectory.GetComputerPrincipal(target.Name);

                    if (p == null)
                    {
                        logger.Trace($"Target computer {target.Name} was not found in the directory");
                        continue;
                    }

                    if (p.Equals(computer))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                        return target;
                    }
                }
                else
                {
                    GroupPrincipal g = activeDirectory.GetGroupPrincipal(target.Name);

                    if (g == null)
                    {
                        logger.Trace($"Target group {target.Name} was not found in the directory");
                        continue;
                    }

                    if (activeDirectory.IsPrincipalInGroup(computer, g))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
            }

            return matchingTargets.OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group).FirstOrDefault();
        }
    }
}