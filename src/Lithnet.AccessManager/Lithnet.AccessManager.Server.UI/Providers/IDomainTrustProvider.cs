using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public interface IDomainTrustProvider
    {
        IList<Domain> GetDomains();

        IList<Forest> GetForests(bool ignoreError = true);

        IList<Domain> GetDomains(Forest forest);
    }
}