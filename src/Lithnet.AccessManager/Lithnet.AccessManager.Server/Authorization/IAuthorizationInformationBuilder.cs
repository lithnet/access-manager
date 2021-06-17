using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationInformationBuilder
    {
        void ClearCache(IUser user, IComputer computer);

        Task<AuthorizationInformation> GetAuthorizationInformation(IUser user, IComputer computer);

        Task<AuthorizationInformation> BuildAuthorizationInformation(IUser user, IComputer computer, IList<SecurityDescriptorTarget> matchedComputerTargets);
    }
}