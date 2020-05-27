using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Config
{
    public interface ILapsConfig
    {
        TargetCollection Targets { get; }

        RateLimitIPElement RateLimitIP { get; }

        RateLimitUserElement RateLimitUser { get; }

        UsersToNotify UsersToNotify { get; }

        AuditElement Audit { get; }
    }
}
