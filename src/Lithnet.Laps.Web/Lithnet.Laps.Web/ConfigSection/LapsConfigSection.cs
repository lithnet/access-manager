using System.Configuration;

namespace Lithnet.Laps.Web
{
    /// <summary>
    /// Singleton for the laps-web-section in the configuration file.
    ///
    /// FIXME: Something is wrong about this class.
    /// For the moment, you have to access its properties via the Configuration property.
    /// So: lapsConfigSection.Configuration.Targets, and not lapsConfigSection.Targets.
    /// </summary>
    public sealed class LapsConfigSection : ConfigurationSection
    {
        private const string SectionName = "lithnet-laps";
        private const string PropTargets = "targets";
        private const string PropTarget = "target";
        private const string PropAudit = "audit";
        private const string PropRateLimitIP = "rate-limit-ip";
        private const string PropRateLimitUser = "rate-limit-user";

        private static readonly LapsConfigSection configuration;

        [ConfigurationProperty(PropTargets)]
        [ConfigurationCollection(typeof(TargetCollection), AddItemName = PropTarget, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public TargetCollection Targets => (TargetCollection)this[PropTargets];

        [ConfigurationProperty(LapsConfigSection.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[LapsConfigSection.PropAudit];

        [ConfigurationProperty(PropRateLimitIP, IsRequired = false)]
        public RateLimitIPElement RateLimitIP => (RateLimitIPElement)this[PropRateLimitIP];

        [ConfigurationProperty(PropRateLimitUser, IsRequired = false)]
        public RateLimitUserElement RateLimitUser => (RateLimitUserElement)this[PropRateLimitUser];

        public LapsConfigSection Configuration
        {
            get { return configuration; }
        }

        static LapsConfigSection()
        {
            configuration = (LapsConfigSection)ConfigurationManager.GetSection(SectionName) ?? new LapsConfigSection();
        }
    }
}