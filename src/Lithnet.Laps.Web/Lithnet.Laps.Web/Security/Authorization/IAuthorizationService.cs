using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization
{
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check whether a given <paramref name="user"/> can
        /// access the password of the <paramref name="computer"/>.
        /// </summary>
        /// <param name="user">a user name</param>
        /// <param name="computer">the computer</param>
        /// <param name="target">the target (as given in config file) containing the computer.</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object, indicating whether the <paramref name="user"/>
        /// can access the password for the <paramref name="computer"/>, in the context of the given
        /// <paramref name="target"/>.</returns>
        AuthorizationResponse CanAccessPassword(IUser user, IComputer computer, ITarget target);
    }
}
