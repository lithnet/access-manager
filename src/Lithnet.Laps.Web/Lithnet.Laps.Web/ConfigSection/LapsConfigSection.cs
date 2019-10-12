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
        
        [ConfigurationProperty(LapsConfigSection.PropTargets)]
        [ConfigurationCollection(typeof(TargetCollection), AddItemName = LapsConfigSection.PropTarget, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public TargetCollection Targets => (TargetCollection)this[LapsConfigSection.PropTargets];

        [ConfigurationProperty(LapsConfigSection.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[LapsConfigSection.PropAudit];

        [ConfigurationProperty(LapsConfigSection.PropRateLimitIP, IsRequired = false)]
        public RateLimitIPElement RateLimitIP => (RateLimitIPElement)this[LapsConfigSection.PropRateLimitIP];

        [ConfigurationProperty(LapsConfigSection.PropRateLimitUser, IsRequired = false)]
        public RateLimitUserElement RateLimitUser => (RateLimitUserElement)this[LapsConfigSection.PropRateLimitUser];

        public UsersToNotify UsersToNotify => this.Audit?.UsersToNotify;
    }
}