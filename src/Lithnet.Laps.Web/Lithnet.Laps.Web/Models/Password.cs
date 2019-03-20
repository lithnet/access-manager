using System;

namespace Lithnet.Laps.Web.Models
{
    public class Password
    {
        public string Value { get; private set; }
        public DateTime? ExpirationTime { get; private set; }

        public Password(string value, DateTime? expirationTime)
        {
            Value = value;
            ExpirationTime = expirationTime;
        }
    }
}