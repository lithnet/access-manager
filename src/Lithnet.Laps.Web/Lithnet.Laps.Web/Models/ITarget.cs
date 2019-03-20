using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Models
{
    public interface ITarget
    {
        TargetType TargetType { get; }
        string TargetName { get; }
        /// <summary>
        /// Password expiration time, formatted as HH:MM:ss.
        ///
        /// FIXME: I guess there is a better type than string to represent this.
        /// </summary>
        string ExpireAfter { get; }
        UsersToNotify UsersToNotify { get; }
    }
}
