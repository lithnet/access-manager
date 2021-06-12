using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class PasswordPolicyOptions
    {
        public int PasswordLength { get; set; }

        public string PasswordCharacters { get; set; }

        public bool UseUpper { get; set; }

        public bool UseLower { get; set; }

        public bool UseSymbol { get; set; }

        public bool UseNumeric { get; set; }

        public int MaximumPasswordAge { get; set; }
    }
}
