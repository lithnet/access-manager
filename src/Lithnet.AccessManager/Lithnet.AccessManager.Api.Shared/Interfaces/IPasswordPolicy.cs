namespace Lithnet.AccessManager
{
    public interface IPasswordPolicy
    {
        int PasswordLength { get; }

        string PasswordCharacters { get; }

        bool UseUpper { get; }

        bool UseLower { get; }

        bool UseSymbol { get; }

        bool UseNumeric { get; }

        int MaximumPasswordAgeDays { get; }

        int MinimumNumberOfPasswords { get; }

        int MinimumPasswordHistoryAgeDays { get; }
    }
}