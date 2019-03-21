using NLog;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IAvailableReaders availableReaders;

        public ConfigurationFileAuthorizationService(ILogger logger,
            IDirectory directory, IAvailableReaders availableReaders)
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

            var readers = availableReaders.GetReadersForTarget(target);

            foreach (var reader in readers)
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.TargetName}");

                    return AuthorizationResponse.Authorized(reader.Audit?.UsersToNotify, reader.Principal);
                }
            }

            return AuthorizationResponse.NoReader();
        }

        private bool IsReaderAuthorized(IReaderElement reader, IUser user)
        {
            var readerAsUser = directory.GetUser(reader.Principal);

            if (readerAsUser != null && readerAsUser.DistinguishedName == user.DistinguishedName)
            {
                return true;
            }

            var readerAsGroup = directory.GetGroup(reader.Principal);

            return readerAsGroup != null && directory.IsUserInGroup(user, readerAsGroup);
        }
    }
}