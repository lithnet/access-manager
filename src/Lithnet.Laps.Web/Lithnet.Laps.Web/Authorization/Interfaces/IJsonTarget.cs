using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IJsonTarget
    {
        IList<IAce> Acl { get; }
        
        IAuditNotificationChannels NotificationChannels { get; }
        
        TimeSpan ExpireAfter { get; }
        
        string Name { get; }
        
        string Sid { get; }

        TargetType Type { get; }
    }
}