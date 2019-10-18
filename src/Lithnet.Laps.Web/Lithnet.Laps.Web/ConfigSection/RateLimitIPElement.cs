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

        [ConfigurationProperty(RateLimitIPElement.PropEnabled, IsRequired = false, DefaultValue = true)]
        public bool Enabled => (bool) this[RateLimitIPElement.PropEnabled];

        [ConfigurationProperty(RateLimitIPElement.PropReqPerMinute, IsRequired = false, DefaultValue = 10)]
        public int ReqPerMinute => (int) this[RateLimitIPElement.PropReqPerMinute];

        [ConfigurationProperty(RateLimitIPElement.PropReqPerHour, IsRequired = false, DefaultValue = 50)]
        public int ReqPerHour => (int) this[RateLimitIPElement.PropReqPerHour];

        [ConfigurationProperty(RateLimitIPElement.PropReqPerDay, IsRequired = false, DefaultValue = 100)]
        public int ReqPerDay => (int) this[RateLimitIPElement.PropReqPerDay];

        [ConfigurationProperty(RateLimitIPElement.PropThrottleOnXffIP, IsRequired = false, DefaultValue = false)]
        public bool ThrottleOnXffIP => (bool)this[RateLimitIPElement.PropThrottleOnXffIP];
    }
}