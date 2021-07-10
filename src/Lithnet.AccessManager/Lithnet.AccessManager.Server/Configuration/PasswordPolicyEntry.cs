namespace Lithnet.AccessManager.Api
{
    public class PasswordPolicyEntry : IPasswordPolicy
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int MinimumNumberOfPasswords { get; set; } = 30;

        public int MinimumPasswordHistoryAgeDays { get; set; } = 0;

        public int MaximumPasswordAgeDays { get; set; } = 7;

        public int PasswordLength { get; set; } = 16;

        public string PasswordCharacters { get; set; }

        public bool UseUpper { get; set; }

        public bool UseLower { get; set; }

        public bool UseSymbol { get; set; }

        public bool UseNumeric { get; set; }

        public string TargetGroup { get; set; }

        public string TargetGroupCachedName { get; set; }

        public AuthorityType TargetType { get; set; }
    }
}