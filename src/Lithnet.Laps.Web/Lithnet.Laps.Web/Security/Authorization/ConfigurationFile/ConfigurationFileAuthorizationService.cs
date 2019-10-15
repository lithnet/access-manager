using System;
using System.Collections.Generic;
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
                if (this.IsReaderAuthorized(reader, user))
                {
                    this.logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.TargetName}");

                    return AuthorizationResponse.Authorized(reader.Audit?.UsersToNotify, reader.Principal);
                }
            }

            this.logger.Trace($"User {user.SamAccountName} did not match any reader principal rules for target {target.TargetName}");

            return AuthorizationResponse.NoReader();
        }

        private bool IsReaderAuthorized(IReaderElement reader, IUser user)
        {
            try
            {
                IUser readerAsUser = this.directory.GetUser(reader.Principal);
                this.logger.Trace($"Reader principal {reader.Principal} found in directory as user {readerAsUser.DistinguishedName}");

                if (readerAsUser.Sid == user.Sid)
                {
                    return true;
                }
                else
                {
                    this.logger.Trace($"Reader principal {reader.Principal} does not match current user {user.SamAccountName}");
                }
            }
            catch (NotFoundException)
            {
            }

            try
            {
                IGroup readerAsGroup = this.directory.GetGroup(reader.Principal);
                this.logger.Trace($"Reader principal {reader.Principal} found in directory as {readerAsGroup.DistinguishedName}");

                if (this.directory.IsUserInGroup(user, readerAsGroup))
                {
                    return true;
                }
                else
                {
                    this.logger.Trace($"Current user {user.SamAccountName} is not a member of the group reader principal {reader.Principal}");
                    return false;
                }
            }
            catch (NotFoundException)
            {
            }

            this.logger.Trace($"Could not translate reader principal {reader.Principal} to a directory object");
            return false;
        }
    }
}