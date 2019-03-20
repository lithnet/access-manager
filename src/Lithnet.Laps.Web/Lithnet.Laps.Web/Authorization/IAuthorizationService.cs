using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check whether the user with given <paramref name="userName"/> can 
        /// access the password of the <paramref name="computer"/>.
        /// </summary>
        /// <param name="userName">a user name</param>
        /// <param name="computer">the computer</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        AuthorizationResponse CanAccessPassword(string userName, IComputer computer);
    }
}
