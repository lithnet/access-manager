namespace Lithnet.AccessManager.Agent
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
    }
}