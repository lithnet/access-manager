using System;

namespace Lithnet.AccessManager.Web.Models
{
    public class PasswordData
    {
        public string Value { get; private set; }

        public DateTime? ExpirationTime { get; private set; }

        public PasswordData(string value, DateTime? expirationTime)
        {
            this.Value = value;
            this.ExpirationTime = expirationTime;
        }
    }
}