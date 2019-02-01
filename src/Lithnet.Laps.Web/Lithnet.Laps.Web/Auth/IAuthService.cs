using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Check whether the user with name <paramref name="userName"/> can 
        /// access the password of the computer with name
        /// <paramref name="computerName"/>
        /// </summary>
        /// <param name="userName">name of the user</param>
        /// <param name="computerName">name of the computer</param>
        /// <param name="target">Target section in the web.config-file.
        /// FIXME: This shouldn't be in this interface. But I can't leave it out
        /// yet, because the code figuring out the target has way too many dependencies.
        /// </param>
        /// <returns><c>true</c> or <c>false</c> accordingly</returns>
        bool CanAccessPassword(string userName, string computerName, TargetElement target = null);
    }
}
