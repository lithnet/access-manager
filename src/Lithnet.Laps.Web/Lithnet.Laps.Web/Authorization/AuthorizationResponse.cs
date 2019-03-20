using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class AuthorizationResponse
    {
        /// <summary>
        /// One of the <see cref="EventIDs"/>.
        /// </summary>
        public int ResultCode { get; private set; }

        public bool IsAuhtorized => ResultCode == EventIDs.UserAuthorizedForComputer;

        public ITarget Target { get; private set; }

        private AuthorizationResponse(int resultCode, UsersToNotify usersToNotify, ITarget target)
        {
            ResultCode = resultCode;
            UsersToNotify = usersToNotify;
            Target = target;
        }

        public static AuthorizationResponse Authorized(UsersToNotify usersToNotify, ITarget target)
        {
            return new AuthorizationResponse(EventIDs.UserAuthorizedForComputer, usersToNotify, target);
        }

        public static AuthorizationResponse NoTarget(UsersToNotify usersToNotify)
        {
            return new AuthorizationResponse(EventIDs.AuthZFailedNoTargetMatch, usersToNotify, null);
        }

        public static AuthorizationResponse NoReader(UsersToNotify usersToNotify, ITarget target)
        {
            return new AuthorizationResponse(EventIDs.AuthZFailedNoReaderPrincipalMatch, usersToNotify,
                target);
        }

        public static AuthorizationResponse Unauthorized(UsersToNotify usersToNotify)
        {
            return new AuthorizationResponse(EventIDs.AuthorizationFailed, usersToNotify, null);
        }

        /// <summary>
        /// Depending on the way the user is authorized, different people can be notified.
        ///
        /// Not sure whether this is a good idea. But it was like this initially.
        /// </summary>
        public UsersToNotify UsersToNotify { get; private set; }

        /// <summary>
        /// This can be anything, offering more information about the user.
        /// </summary>
        public string UserDetails { get; private set; }
    }
}
