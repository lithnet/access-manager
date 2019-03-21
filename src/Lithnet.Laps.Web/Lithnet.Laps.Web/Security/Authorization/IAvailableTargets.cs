using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile
{
    public interface IAvailableTargets
    {
        ITarget GetMatchingTargetOrNull(IComputer computer);
    }
}
