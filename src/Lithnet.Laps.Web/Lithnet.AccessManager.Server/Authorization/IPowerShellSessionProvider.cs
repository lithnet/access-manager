using System.Management.Automation;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IPowerShellSessionProvider
    {
        PowerShell GetSession(string script, params string[] expectedFunctions);
    }
}