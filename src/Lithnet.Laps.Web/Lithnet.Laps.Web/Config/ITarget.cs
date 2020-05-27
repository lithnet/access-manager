using System;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Config
{
    public interface ITarget
    {
        TargetType TargetType { get; }

        string TargetName { get; }

        TimeSpan ExpireAfter { get; }

        UsersToNotify UsersToNotify { get; }
    }
}
