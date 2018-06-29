using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web
{
    public class LapsConfigSection : ConfigurationSection
    {
        private const string SectionName = "lithnet-laps";
        private const string PropTargets = "targets";
        private const string PropTarget = "target";
        private const string PropAudit = "audit";
        private const string PropRateLimitIP = "rate-limit-ip";
        private const string PropRateLimitUser = "rate-limit-user";
        

        [ConfigurationProperty(PropTargets)]
        [ConfigurationCollection(typeof(TargetCollection), AddItemName = PropTarget, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public TargetCollection Targets => (TargetCollection)this[PropTargets];

        internal static LapsConfigSection GetConfiguration()
        {
            LapsConfigSection section = (LapsConfigSection)ConfigurationManager.GetSection(SectionName);

            if (section == null)
            {
                section = new LapsConfigSection();
            }

            return section;
        }

        [ConfigurationProperty(LapsConfigSection.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[LapsConfigSection.PropAudit];

        [ConfigurationProperty(PropRateLimitIP, IsRequired = false)]
        public RateLimitIPElement RateLimitIP => (RateLimitIPElement)this[PropRateLimitIP];

        [ConfigurationProperty(PropRateLimitUser, IsRequired = false)]
        public RateLimitUserElement RateLimitUser => (RateLimitUserElement)this[PropRateLimitUser];

        internal static LapsConfigSection Configuration { get; private set; }

        static LapsConfigSection()
        {
            LapsConfigSection.Configuration = LapsConfigSection.GetConfiguration();
        }
    }
}