using System.Management.Automation;

namespace Lithnet.AccessManager.Server
{
    public interface IPowerShellSessionProvider
    {
        PowerShell GetSession(string script, params string[] expectedFunctions);
    }
}