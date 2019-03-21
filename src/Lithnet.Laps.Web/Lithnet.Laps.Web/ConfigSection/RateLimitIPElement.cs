using System.Configuration;

namespace Lithnet.Laps.Web
{
    public class RateLimitIPElement : ConfigurationElement
    {
        private const string PropEnabled = "enabled";
        private const string PropReqPerMinute = "requestsPerMinute";
        private const string PropReqPerHour = "requestsPerHour";
        private const string PropReqPerDay = "requestsPerDay";
        private const string PropThrottleOnXffIP = "rateLimitOnXffIP";

        [ConfigurationProperty(PropEnabled, IsRequired = false, DefaultValue = true)]
        public bool Enabled => (bool) this[PropEnabled];

        [ConfigurationProperty(PropReqPerMinute, IsRequired = false, DefaultValue = 10)]
        public int ReqPerMinute => (int) this[PropReqPerMinute];

        [ConfigurationProperty(PropReqPerHour, IsRequired = false, DefaultValue = 50)]
        public int ReqPerHour => (int) this[PropReqPerHour];

        [ConfigurationProperty(PropReqPerDay, IsRequired = false, DefaultValue = 100)]
        public int ReqPerDay => (int) this[PropReqPerDay];

        [ConfigurationProperty(PropThrottleOnXffIP, IsRequired = false, DefaultValue = false)]
        public bool ThrottleOnXffIP => (bool)this[PropThrottleOnXffIP];
    }
}