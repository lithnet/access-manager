using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public abstract class ImportSettings
    {
        public ImportMode ImportMode { get; set; }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public bool AllowLaps { get; set; }

        public bool AllowLapsHistory { get; set; }

        public bool AllowJit { get; set; }

        public bool AllowBitLocker { get; set; }

        public string RuleDescription { get; set; }

        public string JitAuthorizingGroup { get; set; }

        public TimeSpan JitExpireAfter { get; set; }

        public TimeSpan LapsExpireAfter { get; set; }

        public HashSet<SecurityIdentifier> PrincipalFilter { get; } = new HashSet<SecurityIdentifier>();

        public AuditNotificationChannels Notifications { get; set; }
    }
}
