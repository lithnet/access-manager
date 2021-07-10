using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class PasswordPolicyOptions
    {
        public int RollbackWindowMinutes { get; set; } = 1;

        public string EncryptionCertificateThumbprint { get; set; }

        public int PolicyCacheDurationSeconds { get; set; } = 60;

        public PasswordPolicyEntry DefaultPolicy { get; set; } = new PasswordPolicyEntry() { Id = "Default", Name = "Default policy" };

        public List<PasswordPolicyEntry> Policies { get; set; } = new List<PasswordPolicyEntry>();
    }
}