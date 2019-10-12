using System;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web.Models
{
    public interface ITarget
    {
        TargetType TargetType { get; }

        string TargetName { get; }
        
        TimeSpan ExpireAfter { get; }

        UsersToNotify UsersToNotify { get; }
    }
}
