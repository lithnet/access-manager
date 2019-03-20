using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAvailableTargets
    {
        ITarget GetMatchingTargetOrNull(IComputer computer);
    }
}
