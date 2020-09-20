using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface IComputerPrincipalProvider
    {
        List<SecurityIdentifier> GetPrincipalsForComputer(SearchResult computer, bool filterLocalAccounts);

        List<string> ComputerPropertiesToGet { get; }
    }
}