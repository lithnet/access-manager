namespace Lithnet.Laps.Web.Models
{
    public interface ILapsConfig
    {
        TargetCollection Targets { get; }
        AuditElement Audit { get; }
        RateLimitIPElement RateLimitIP { get; }
        RateLimitUserElement RateLimitUser { get; }
    }
}
