using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class PasswordPolicyOptions
    {
        public int PasswordLength { get; set; } = 16;

        public string PasswordCharacters { get; set; }

        public bool UseUpper { get; set; } = true;

        public bool UseLower { get; set; } = true;

        public bool UseSymbol { get; set; } = true;

        public bool UseNumeric { get; set; } = true;

        public int MaximumPasswordAgeDays { get; set; } = 7;
    }
}
