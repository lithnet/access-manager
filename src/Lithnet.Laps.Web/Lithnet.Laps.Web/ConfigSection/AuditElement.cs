using System.Configuration;
using Lithnet.Laps.Web.Audit;

namespace Lithnet.Laps.Web
{
    public class AuditElement : ConfigurationElement
    {
        private const string PropNotifySuccess = "emailOnSuccess";
        private const string PropNotifyFailure = "emailOnFailure";

        private const string PropEmailAddresses = "emailAddresses";

        [ConfigurationProperty(PropNotifySuccess, IsRequired = false, DefaultValue = true)]
        public bool NotifySuccess => (bool) this[PropNotifySuccess];

        [ConfigurationProperty(PropNotifyFailure, IsRequired = false, DefaultValue = true)]
        public bool NotifyFailure => (bool)this[PropNotifyFailure];
 
        [ConfigurationProperty(PropEmailAddresses, IsRequired = false)]
        public string EmailAddresses => (string) this[PropEmailAddresses];

        public UsersToNotify UsersToNotify
        {
            get
            {
                var result = new UsersToNotify();

                if (NotifySuccess)
                {
                    result = result.NotifyOnSuccess(EmailAddresses);
                }

                if (NotifyFailure)
                {
                    result = result.NotifyOnFailure(EmailAddresses);
                }

                return result;
            }
        }
    }
}