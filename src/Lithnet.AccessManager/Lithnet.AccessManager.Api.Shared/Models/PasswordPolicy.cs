using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    public class PasswordPolicy : IPasswordPolicy
    {
        public PasswordPolicy()
        {
        }

        public PasswordPolicy(IPasswordPolicy policy)
        {
            PasswordLength = policy.PasswordLength;
            MaximumPasswordAgeDays = policy.MaximumPasswordAgeDays;
            PasswordCharacters = policy.PasswordCharacters;
            UseLower = policy.UseLower;
            UseNumeric = policy.UseNumeric;
            UseSymbol = policy.UseSymbol;
            UseUpper = policy.UseUpper;
            MinimumNumberOfPasswords = policy.MinimumNumberOfPasswords;
            MinimumPasswordHistoryAgeDays = policy.MinimumPasswordHistoryAgeDays;
        }

        public int PasswordLength { get; set; } = 16;

        public string PasswordCharacters { get; set; }

        public bool UseUpper { get; set; } = true;

        public bool UseLower { get; set; } = true;

        public bool UseSymbol { get; set; } = false;

        public bool UseNumeric { get; set; } = true;

        public int MaximumPasswordAgeDays { get; set; } = 7;

        public int MinimumNumberOfPasswords { get; set; } = 2;

        public int MinimumPasswordHistoryAgeDays { get; set; } = 30;
    }
}