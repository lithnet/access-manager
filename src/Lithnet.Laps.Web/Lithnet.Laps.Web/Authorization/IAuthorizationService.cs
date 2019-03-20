using System.DirectoryServices.AccountManagement;
using Lithnet.Laps.Web.Models;
using Microsoft.VisualBasic.Devices;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check whether the user with name <paramref name="currentUser"/> can 
        /// access the password of the <paramref name="computer"/>.
        /// </summary>
        /// <param name="currentUser">a user. FIXME: this should just be the user name.</param>
        /// <param name="computer">the computer</param>
        /// <returns>An <see cref="AuthorizationResponse"/> object.</returns>
        AuthorizationResponse CanAccessPassword(UserPrincipal currentUser, IComputer computer);
    }
}
