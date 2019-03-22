using System.Configuration;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public sealed class LapsConfigSection : ConfigurationSection, ILapsConfig
    {
        public const string SectionName = "lithnet-laps";
        private const string PropTargets = "targets";
        private const string PropTarget = "target";
        private const string PropAudit = "audit";
        private const string PropRateLimitIP = "rate-limit-ip";
        private const string PropRateLimitUser = "rate-limit-user";

        [ConfigurationProperty(PropTargets)]
        [ConfigurationCollection(typeof(TargetCollection), AddItemName = PropTarget, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public TargetCollection Targets => (TargetCollection)this[PropTargets];

        [ConfigurationProperty(LapsConfigSection.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[LapsConfigSection.PropAudit];

        [ConfigurationProperty(PropRateLimitIP, IsRequired = false)]
        public RateLimitIPElement RateLimitIP => (RateLimitIPElement)this[PropRateLimitIP];

        [ConfigurationProperty(PropRateLimitUser, IsRequired = false)]
        public RateLimitUserElement RateLimitUser => (RateLimitUserElement)this[PropRateLimitUser];

        public UsersToNotify UsersToNotify => Audit?.UsersToNotify;
    }
}