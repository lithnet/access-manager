using System.Collections.Generic;

namespace Lithnet.Laps.Web.JsonTargets
{
    public interface IJsonTargetsProvider
    {
        IList<JsonTarget> Targets { get; }
    }
}