using System.Security.AccessControl;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IPowerShellSecurityDescriptorGenerator
    {
        CommonSecurityDescriptor GenerateSecurityDescriptor(IUser user, IComputer computer, string script, int timeout);
    }
}