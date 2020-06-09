using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IJsonTargetsProvider
    {
        IList<IJsonTarget> Targets { get; }
    }
}