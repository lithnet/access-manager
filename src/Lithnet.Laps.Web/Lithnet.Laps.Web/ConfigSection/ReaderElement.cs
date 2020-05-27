using System.Configuration;
using Lithnet.Laps.Web.Config;

namespace Lithnet.Laps.Web
{
    public class ReaderElement : ConfigurationElement, IReaderElement
    {
        private const string PropAudit = "audit";
        private const string PropPrincipal = "principal";

        [ConfigurationProperty(ReaderElement.PropAudit, IsRequired = false)]
        public AuditElement Audit => (AuditElement)this[ReaderElement.PropAudit];

        [ConfigurationProperty(ReaderElement.PropPrincipal, IsRequired = true, IsKey = true)]
        public string Principal => (string)this[ReaderElement.PropPrincipal];
    }
}