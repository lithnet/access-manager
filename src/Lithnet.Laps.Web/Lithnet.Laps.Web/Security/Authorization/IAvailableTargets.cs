using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization
{
    public interface IAvailableTargets
    {
        ITarget GetMatchingTargetOrNull(IComputer computer);
    }
}
