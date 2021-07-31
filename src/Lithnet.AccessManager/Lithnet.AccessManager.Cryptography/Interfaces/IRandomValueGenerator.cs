namespace Lithnet.AccessManager.Cryptography
{
    public interface IRandomValueGenerator
    {
        string GenerateRandomString(int length);

        string GenerateRandomString(int length, string specificCharacters);

        string GenerateRandomString(int length, bool useLower, bool useUpper, bool useNumeric, bool useSymbol);
    }
}