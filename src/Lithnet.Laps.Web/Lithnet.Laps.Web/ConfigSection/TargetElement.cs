using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.DirectoryServices.AccountManagement;

namespace Lithnet.Laps.Web
{
    public class TargetElement : ConfigurationElement
    {
        private Principal principal;

        private const string PropAudit = "audit";
        private const string PropReaders = "readers";
        private const string PropReader = "reader";
        private const string PropExpireAfter = "expire-after";
        private const string PropID = "name";
        private const string PropIDType = "type";

        [ConfigurationProperty(TargetElement.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement) this[TargetElement.PropAudit];

        [ConfigurationProperty(PropIDType, IsRequired = false)]
        public TargetType Type => (TargetType) this[PropIDType];

        [ConfigurationProperty(PropID, IsRequired = true, IsKey = true)]
        public string Name => (string) this[PropID];

        [ConfigurationProperty(TargetElement.PropExpireAfter, IsRequired = false)]
        public string ExpireAfter => (string) this[TargetElement.PropExpireAfter];

        [ConfigurationProperty(PropReaders, IsRequired = true)]
        [ConfigurationCollection(typeof(ReaderCollection), AddItemName = PropReader, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public ReaderCollection Readers => (ReaderCollection) this[PropReaders];

        internal Principal PrincipalObject
        {
            get
            {
                if (this.principal == null)
                {
                    PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
                    this.principal = Principal.FindByIdentity(ctx, this.Name);
                }

                return this.principal;
            }
        }
    }
}