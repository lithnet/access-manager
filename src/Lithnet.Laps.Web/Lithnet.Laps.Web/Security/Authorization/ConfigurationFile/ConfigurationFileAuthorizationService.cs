using System;
using System.Collections.Generic;
using System.Security.Principal;
using Lithnet.Laps.Web.Config;
using NLog;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IAvailableReaders availableReaders;

        public ConfigurationFileAuthorizationService(ILogger logger, IDirectory directory, IAvailableReaders availableReaders)
        {
            this.logger = logger;
            this.directory = directory;
            this.availableReaders = availableReaders;
        }

        public AuthorizationResponse CanAccessPassword(IUser user, IComputer computer, ITarget target)
        {
            if (target == null)
            {
                return AuthorizationResponse.NoTarget();
            }

            foreach (IReaderElement reader in this.availableReaders.GetReadersForTarget(target))
            {
                if (this.IsReaderAuthorized(reader, computer, user))
                {
                    this.logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.TargetName}");

                    return AuthorizationResponse.Authorized(reader.Audit?.UsersToNotify, reader.Principal);
                }
            }

            this.logger.Trace($"User {user.SamAccountName} did not match any reader principal rules for target {target.TargetName}");

            return AuthorizationResponse.NoReader();
        }

        private bool IsReaderAuthorized(IReaderElement reader, IComputer computer,  IUser user)
        {
            try
            {
                ISecurityPrincipal principal = this.directory.GetPrincipal(reader.Principal);

                this.logger.Trace($"Reader principal {reader.Principal} found in directory as user {principal.DistinguishedName}");

                if (this.directory.IsSidInPrincipalToken(computer.Sid.AccountDomainSid, user, principal.Sid))
                {
                    return true;
                }
                else
                {
                    this.logger.Trace($"Reader principal {reader.Principal} does not match current user {user.SamAccountName}");
                    return false;
                }
            }
            catch (NotFoundException)
            {
            }

            this.logger.Trace($"Could not match reader principal {reader.Principal} to a directory object");
            return false;
        }
    }
}