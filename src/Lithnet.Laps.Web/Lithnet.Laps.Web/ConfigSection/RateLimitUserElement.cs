using System.Configuration;

namespace Lithnet.Laps.Web
{
    public class RateLimitUserElement : ConfigurationElement
    {
        private const string PropEnabled = "enabled";
        private const string PropReqPerMinute = "requestsPerMinute";
        private const string PropReqPerHour = "requestsPerHour";
        private const string PropReqPerDay = "requestsPerDay";

        [ConfigurationProperty(RateLimitUserElement.PropEnabled, IsRequired = false, DefaultValue = true)]
        public bool Enabled => (bool) this[RateLimitUserElement.PropEnabled];

        [ConfigurationProperty(RateLimitUserElement.PropReqPerMinute, IsRequired = false, DefaultValue = 10)]
        public int ReqPerMinute => (int) this[RateLimitUserElement.PropReqPerMinute];

        [ConfigurationProperty(RateLimitUserElement.PropReqPerHour, IsRequired = false, DefaultValue = 50)]
        public int ReqPerHour => (int) this[RateLimitUserElement.PropReqPerHour];

        [ConfigurationProperty(RateLimitUserElement.PropReqPerDay, IsRequired = false, DefaultValue = 100)]
        public int ReqPerDay => (int) this[RateLimitUserElement.PropReqPerDay];
    }
}