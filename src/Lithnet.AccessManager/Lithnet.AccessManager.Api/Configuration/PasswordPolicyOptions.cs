using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class PasswordPolicyOptions
    {
        public int MaxNumberOfPasswords { get; set; } = 30;

        public int MaximumPasswordHistoryAgeDays { get; set; } = 0;

        public int MaximumPasswordAgeDays { get; set; } = 7;

        public int RollbackWindowMinutes { get; set; } = 1;
    }
}
