namespace Lithnet.AccessManager
{
    public interface IPasswordPolicy
    {
        public int PasswordLength { get; }

        public string PasswordCharacters { get; }

        public bool UseUpper { get; }

        public bool UseLower { get; }

        public bool UseSymbol { get; }

        public bool UseNumeric { get; }

        public int MaximumPasswordAgeDays { get; }

        public int MinimumNumberOfPasswords { get; }

        public int MinimumPasswordHistoryAgeDays { get; }
    }
}