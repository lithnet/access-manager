using System;
using System.Configuration;
using System.Globalization;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public class TargetElement : ConfigurationElement, ITarget
    {
        private bool setTimeSpan;
        private TimeSpan expireAfter;

        private const string PropAudit = "audit";
        private const string PropReaders = "readers";
        private const string PropReader = "reader";
        private const string PropExpireAfter = "expire-after";
        private const string PropID = "name";
        private const string PropIDType = "type";

        [ConfigurationProperty(TargetElement.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[TargetElement.PropAudit];

        [ConfigurationProperty(TargetElement.PropIDType, IsRequired = false)]
        public TargetType Type => (TargetType)this[TargetElement.PropIDType];

        [ConfigurationProperty(TargetElement.PropID, IsRequired = true, IsKey = true)]
        public string Name => (string)this[TargetElement.PropID];

        [ConfigurationProperty(TargetElement.PropExpireAfter, IsRequired = false)]
        public string ExpireAfter => (string)this[TargetElement.PropExpireAfter];

        [ConfigurationProperty(TargetElement.PropReaders, IsRequired = true)]
        [ConfigurationCollection(typeof(ReaderCollection), AddItemName = TargetElement.PropReader, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public ReaderCollection Readers => (ReaderCollection)this[TargetElement.PropReaders];

        TargetType ITarget.TargetType => this.Type;

        string ITarget.TargetName => this.Name;

        TimeSpan ITarget.ExpireAfter
        {
            get
            {
                if (!this.setTimeSpan)
                {
                    this.setTimeSpan = true;
                    this.expireAfter = new TimeSpan(0);

                    if (TimeSpan.TryParse(this.ExpireAfter, CultureInfo.InvariantCulture, out TimeSpan t))
                    {
                        this.expireAfter = t;
                    }
                }

                return this.expireAfter;
            }
        }

        UsersToNotify ITarget.UsersToNotify => this.Audit.UsersToNotify;
    }
}