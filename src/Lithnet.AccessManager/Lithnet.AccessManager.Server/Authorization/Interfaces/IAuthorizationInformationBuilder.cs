using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationInformationBuilder
    {
        void ClearCache(IActiveDirectoryUser user, IComputer computer);

        Task<AuthorizationInformation> GetAuthorizationInformation(IActiveDirectoryUser user, IComputer computer);

        Task<AuthorizationInformation> BuildAuthorizationInformation(IActiveDirectoryUser user, IComputer computer, IList<SecurityDescriptorTarget> matchedComputerTargets);
    }
}