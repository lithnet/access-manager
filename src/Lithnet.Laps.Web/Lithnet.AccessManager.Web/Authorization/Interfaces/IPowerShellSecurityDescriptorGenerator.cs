using System.Security.AccessControl;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IPowerShellSecurityDescriptorGenerator
    {
        CommonSecurityDescriptor GenerateSecurityDescriptor(IUser user, IComputer computer, AccessMask requestedAccess, string script, int timeout);
    }
}