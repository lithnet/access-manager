using System.Configuration;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;

namespace Lithnet.Laps.Web
{
    public class AuditElement : ConfigurationElement
    {
        private const string PropNotifySuccess = "emailOnSuccess";

        private const string PropNotifyFailure = "emailOnFailure";

        private const string PropEmailAddresses = "emailAddresses";

        private const string PropReason = "userSuppliedReason";

        [ConfigurationProperty(AuditElement.PropNotifySuccess, IsRequired = false, DefaultValue = true)]
        public bool NotifySuccess => (bool) this[AuditElement.PropNotifySuccess];

        [ConfigurationProperty(AuditElement.PropNotifyFailure, IsRequired = false, DefaultValue = true)]
        public bool NotifyFailure => (bool)this[AuditElement.PropNotifyFailure];

        [ConfigurationProperty(AuditElement.PropReason, IsRequired = false, DefaultValue = AuditReasonFieldState.Optional)]
        public AuditReasonFieldState Reason => (AuditReasonFieldState)this[AuditElement.PropReason];

        [ConfigurationProperty(AuditElement.PropEmailAddresses, IsRequired = false)]
        public string EmailAddresses => (string) this[AuditElement.PropEmailAddresses];

        public UsersToNotify UsersToNotify
        {
            get
            {
                UsersToNotify result = new UsersToNotify();

                if (this.NotifySuccess)
                {
                    result = result.NotifyOnSuccess(this.EmailAddresses);
                }

                if (this.NotifyFailure)
                {
                    result = result.NotifyOnFailure(this.EmailAddresses);
                }

                return result;
            }
        }
    }
}