using NLog;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.Auth
{
    public class ConfigurationFileAuthService : IAuthService
    {
        // FIXME: Use dependency injection to inject the logger.
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Check whether the user with name <paramref name="userName"/> can 
        /// access the password of the computer with name
        /// <paramref name="computerName"/>
        /// </summary>
        /// <param name="currentUser">a user. FIXME: We shouldn't depend on AD here.</param>
        /// <param name="computerName">name of the computer</param>
        /// <param name="target">Target section in the web.config-file.
        /// FIXME: This shouldn't be in this interface. But I can't leave it out
        /// yet, because the code figuring out the target has way too many dependencies.
        /// </param>
        /// <returns>An <see cref="AuthResponse"/> object.</returns>
        public AuthResponse CanAccessPassword(UserPrincipal user, string computerName, TargetElement target = null)
        {
            // FIXME: This function doesn't even look at computerName, because it assumes this check already happened at some other place.
            foreach (ReaderElement reader in target.Readers.OfType<ReaderElement>())
            {
                if (this.IsReaderAuthorized(reader, user))
                {
                    logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.Name}");

                    return new AuthResponse(true, reader);
                }
            }

            return new AuthResponse(false, null);
        }

        private bool IsReaderAuthorized(ReaderElement reader, UserPrincipal currentUser)
        {
            Principal readerPrincipal = Directory.GetPrincipal(reader.Principal);

            if (currentUser.Equals(readerPrincipal))
            {
                return true;
            }

            if (readerPrincipal is GroupPrincipal group)
            {
                if (Directory.IsPrincipalInGroup(currentUser, group))
                {
                    return true;
                }
            }

            return false;
        }
    }
}