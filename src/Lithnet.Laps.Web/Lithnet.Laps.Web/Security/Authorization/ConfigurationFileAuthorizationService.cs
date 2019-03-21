using System.Collections.Generic;
using NLog;
using System.Linq;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly LapsConfigSection configSection;
        private readonly ILogger logger;
        private readonly IDirectory directory;

        public ConfigurationFileAuthorizationService(LapsConfigSection configSection, ILogger logger,
            IDirectory directory)
        {
            this.configSection = configSection;
            this.logger = logger;
            this.directory = directory;
        }

        public AuthorizationResponse CanAccessPassword(IUser user, IComputer computer, ITarget target)
        {
            if (target == null)
            {
                return AuthorizationResponse.NoTarget();
            }

            var readers = GetReadersForTarget(target);

            foreach (ReaderElement reader in readers)
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.TargetName}");

                    return AuthorizationResponse.Authorized(reader.Audit.UsersToNotify, reader.Principal);
                }
            }

            return AuthorizationResponse.NoReader();
        }

        private IEnumerable<ReaderElement> GetReadersForTarget(ITarget target)
        {
            var targetElementCollection = configSection.Configuration.Targets;
            
            var query = from targetElement in targetElementCollection.OfType<TargetElement>()
                where targetElement.Name == target.TargetName
                select targetElement.Readers;

            var readerCollection = query.FirstOrDefault();

            if (readerCollection == null)
            {
                return new ReaderElement[0];
            }

            return readerCollection.OfType<ReaderElement>();
        }

        private bool IsReaderAuthorized(ReaderElement reader, IUser user)
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