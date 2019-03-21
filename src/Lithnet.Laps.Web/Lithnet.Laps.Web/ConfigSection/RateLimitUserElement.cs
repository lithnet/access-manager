using System.Configuration;

namespace Lithnet.Laps.Web
{
    public class RateLimitUserElement : ConfigurationElement
    {
        private const string PropEnabled = "enabled";
        private const string PropReqPerMinute = "requestsPerMinute";
        private const string PropReqPerHour = "requestsPerHour";
        private const string PropReqPerDay = "requestsPerDay";

        [ConfigurationProperty(PropEnabled, IsRequired = false, DefaultValue = true)]
        public bool Enabled => (bool) this[PropEnabled];

        [ConfigurationProperty(PropReqPerMinute, IsRequired = false, DefaultValue = 10)]
        public int ReqPerMinute => (int) this[PropReqPerMinute];

        [ConfigurationProperty(PropReqPerHour, IsRequired = false, DefaultValue = 50)]
        public int ReqPerHour => (int) this[PropReqPerHour];

        [ConfigurationProperty(PropReqPerDay, IsRequired = false, DefaultValue = 100)]
        public int ReqPerDay => (int) this[PropReqPerDay];
    }
}