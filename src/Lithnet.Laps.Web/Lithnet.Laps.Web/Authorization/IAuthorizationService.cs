using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check whether the user with name <paramref name="currentUser"/> can 
        /// access the password of the computer with name
        /// <paramref name="computerName"/>
        /// </summary>
        /// <param name="currentUser">a user. FIXME: this should just be the user name.</param>
        /// <param name="computerName">name of the computer</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        AuthorizationResponse CanAccessPassword(UserPrincipal currentUser, string computerName);
    }
}
