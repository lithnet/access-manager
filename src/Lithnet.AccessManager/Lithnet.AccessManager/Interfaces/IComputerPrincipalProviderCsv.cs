using System.Collections.Generic;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface IComputerPrincipalProviderCsv : IComputerPrincipalProvider
    {
        void ImportPrincipalMappings(string file, bool hasHeaderRow);

        void ClearPrincipalMappings();
    }
}