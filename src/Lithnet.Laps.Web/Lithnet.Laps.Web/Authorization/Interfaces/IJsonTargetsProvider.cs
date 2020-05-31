using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IJsonTargetsProvider
    {
        IList<JsonTarget> Targets { get; }
    }
}