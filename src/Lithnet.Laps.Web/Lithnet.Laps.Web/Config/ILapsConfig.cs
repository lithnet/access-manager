using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Config
{
    public interface ILapsConfig
    {
        TargetCollection Targets { get; }
    }
}
