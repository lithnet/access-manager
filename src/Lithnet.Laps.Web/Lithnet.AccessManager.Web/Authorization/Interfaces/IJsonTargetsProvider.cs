using System.Collections.Generic;

namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IJsonTargetsProvider
    {
        IList<IJsonTarget> Targets { get; }
    }
}