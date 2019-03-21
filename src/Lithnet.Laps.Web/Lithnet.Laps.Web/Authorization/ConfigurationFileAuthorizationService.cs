using System.Collections.Generic;
using NLog;
using System.Linq;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class ConfigurationFileAuthorizationService : IAuthorizationService
    {
        private readonly LapsConfigSection configSection;
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IAvailableTargets availableTargets;

        public ConfigurationFileAuthorizationService(LapsConfigSection configSection, ILogger logger,
            IDirectory directory, IAvailableTargets availableTargets)
        {
            this.configSection = configSection;
            this.logger = logger;
            this.directory = directory;
            this.availableTargets = availableTargets;
        }

        public AuthorizationResponse CanAccessPassword(IUser user, IComputer computer)
        {
            var target = availableTargets.GetMatchingTargetOrNull(computer);

            if (target == null)
            {
                return AuthorizationResponse.NoTarget(new UsersToNotify());
            }

            var readers = GetReadersForTarget(target);

            foreach (ReaderElement reader in readers)
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.TargetName}");

                    return AuthorizationResponse.Authorized(((ITarget)target).UsersToNotify, target);
                }
            }

            return AuthorizationResponse.NoReader(((ITarget) target).UsersToNotify, target);
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