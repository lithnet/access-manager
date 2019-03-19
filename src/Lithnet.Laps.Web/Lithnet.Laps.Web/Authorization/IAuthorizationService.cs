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
        /// <param name="target">Target section in the web.config-file.
        /// FIXME: This shouldn't be in this interface. But I can't leave it out
        /// yet, because the code figuring out the target has way too many dependencies.
        /// </param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        AuthorizationResponse CanAccessPassword(UserPrincipal currentUser, string computerName, TargetElement target = null);
    }
}
