using System;
using Lithnet.AccessManager.Cryptography;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class RandomPasswordGenerator : IPasswordGenerator
    {
        private readonly IRandomValueGenerator randomValueGenerator;

        public RandomPasswordGenerator(IRandomValueGenerator rvg)
        {
            this.randomValueGenerator = rvg;
        }

        public string Generate(IPasswordPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(policy.PasswordCharacters))
            {
                return this.randomValueGenerator.GenerateRandomString(Math.Max(policy.PasswordLength, 8), policy.UseLower, policy.UseUpper, policy.UseNumeric, policy.UseSymbol);
            }
            else
            {
                return this.randomValueGenerator.GenerateRandomString(Math.Max(policy.PasswordLength, 8), policy.PasswordCharacters);

            }
        }
    }
}
