using System;

namespace Lithnet.AccessManager.Agent
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
