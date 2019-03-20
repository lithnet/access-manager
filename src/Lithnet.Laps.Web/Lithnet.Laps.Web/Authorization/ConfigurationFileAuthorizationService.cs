using NLog;
using System;
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
        private readonly Logger logger;
        private readonly Directory directory;

        public ConfigurationFileAuthorizationService(LapsConfigSection configSection, Logger logger,
            Directory directory)
        {
            this.configSection = configSection;
            this.logger = logger;
            this.directory = directory;
        }

        /// <summary>
        /// Check whether the user with name <paramref name="userName"/> can 
        /// access the password of the computer with name
        /// <paramref name="computerName"/>, based on the reader elements under the targets in Web.Config.
        /// </summary>
        /// <param name="user">a user. FIXME: We shouldn't depend on AD here.</param>
        /// <param name="computerName">name of the computer</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        public AuthorizationResponse CanAccessPassword(UserPrincipal user, string computerName)
        {
            var computer = directory.GetComputerPrincipal(computerName);
            var target = GetMatchingTargetOrNull(computer);

            if (target == null)
            {
                return new AuthorizationResponse(EventIDs.AuthZFailedNoTargetMatch, new UsersToNotify(), String.Empty);
            }

            foreach (ReaderElement reader in target.Readers.OfType<ReaderElement>())
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.Name}");

                    return new AuthorizationResponse(EventIDs.UserAuthorizedForComputer, reader.Audit.UsersToNotify, reader.Principal);
                }
            }

            return new AuthorizationResponse(EventIDs.AuthZFailedNoReaderPrincipalMatch, new UsersToNotify(), String.Empty);
        }

        private bool IsReaderAuthorized(ReaderElement reader, UserPrincipal currentUser)
        {
            var readerPrincipal = directory.GetPrincipal(reader.Principal);

            if (currentUser.Equals(readerPrincipal))
            {
                return true;
            }

            if (readerPrincipal is GroupPrincipal group)
            {
                if (directory.IsPrincipalInGroup(currentUser, group))
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
                    if (directory.IsPrincipalInOu(computer, target.Name))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                        matchingTargets.Add(target);
                    }

                    continue;
                }
                else if (target.Type == TargetType.Computer)
                {
                    ComputerPrincipal p = directory.GetComputerPrincipal(target.Name);

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
                    GroupPrincipal g = directory.GetGroupPrincipal(target.Name);

                    if (g == null)
                    {
                        logger.Trace($"Target group {target.Name} was not found in the directory");
                        continue;
                    }

                    if (directory.IsPrincipalInGroup(computer, g))
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