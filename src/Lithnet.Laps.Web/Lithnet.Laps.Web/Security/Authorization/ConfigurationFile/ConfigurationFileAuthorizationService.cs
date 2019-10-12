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

            return AuthorizationResponse.NoReader();
        }

        private bool IsReaderAuthorized(IReaderElement reader, IUser user)
        {
            try
            {
                IUser readerAsUser = this.directory.GetUser(reader.Principal);

                if (readerAsUser != null && readerAsUser.Sid == user.Sid)
                {
                    return true;
                }
            }
            catch (NotFoundException)
            {
            }

            try
            {
                IGroup readerAsGroup = this.directory.GetGroup(reader.Principal);
                return readerAsGroup != null && this.directory.IsUserInGroup(user, readerAsGroup);
            }
            catch (NotFoundException)
            {
            }

            return false;
        }
    }
}