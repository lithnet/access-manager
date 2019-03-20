using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check whether a given <paramref name="user"/> can
        /// access the password of the <paramref name="computer"/>.
        /// </summary>
        /// <param name="user">a user name</param>
        /// <param name="computer">the computer</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        AuthorizationResponse CanAccessPassword(IUser user, IComputer computer);
    }
}
