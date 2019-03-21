using System;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization
{
    public sealed class AuthorizationResponse
    {
        /// <summary>
        /// One of the <see cref="EventIDs"/>.
        /// </summary>
        public int ResultCode { get; private set; }

        public bool IsAuthorized => ResultCode == EventIDs.UserAuthorizedForComputer;

        private AuthorizationResponse(int resultCode, UsersToNotify usersToNotify, string extraInfo)
        {
            ResultCode = resultCode;
            UsersToNotify = usersToNotify ?? new UsersToNotify();
            ExtraInfo = extraInfo;
        }

        public static AuthorizationResponse Authorized(UsersToNotify usersToNotify, string extraInfo)
        {
            return new AuthorizationResponse(EventIDs.UserAuthorizedForComputer, usersToNotify, extraInfo);
        }

        public static AuthorizationResponse NoTarget()
        {
            return new AuthorizationResponse(EventIDs.AuthZFailedNoTargetMatch, new UsersToNotify(), String.Empty);
        }

        public static AuthorizationResponse NoReader()
        {
            return new AuthorizationResponse(EventIDs.AuthZFailedNoReaderPrincipalMatch, new UsersToNotify(), String.Empty);
        }

        public static AuthorizationResponse Unauthorized()
        {
            return new AuthorizationResponse(EventIDs.AuthorizationFailed, new UsersToNotify(), String.Empty);
        }

        /// <summary>
        /// Depending on the kind of user that authenticates, other people might want to be notified.
        ///
        /// Note that this should only contain the users that needs to be notified for the specified reader (user or group).
        /// The users that needs notifications for the target (ou of the computer), are defined in <see cref="ITarget."/>
        /// </summary>
        public UsersToNotify UsersToNotify { get; private set; }

        /// <summary>
        /// This can be anything, offering more information about the user or the authentication.
        /// </summary>
        public string ExtraInfo { get; private set; }
    }
}
