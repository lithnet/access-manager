using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Authorization
{
    public sealed class AuthorizationResponse
    {
        public bool Success { get;  private set; }

        public AuthorizationResponse(bool success, UsersToNotify usersToNotify, string userDetails)
        {
            Success = success;
            UserDetails = userDetails;
            UsersToNotify = usersToNotify;
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
