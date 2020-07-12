using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IJsonTarget
    {
        IList<IAce> Acl { get; }

        IAuditNotificationChannels NotificationChannels { get; }

        string Name { get; }

        string Sid { get; }

        TargetType Type { get; }

        JsonTargetLapsDetails Laps { get; }

        JsonTargetJitDetails Jit { get; }
    }
}