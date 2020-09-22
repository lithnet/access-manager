using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationInformationBuilder
    {
        void ClearCache(IUser user, IComputer computer);

        AuthorizationInformation GetAuthorizationInformation(IUser user, IComputer computer);

        AuthorizationInformation BuildAuthorizationInformation(IUser user, IComputer computer, IList<SecurityDescriptorTarget> targets);
    }
}