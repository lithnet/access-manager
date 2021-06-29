using Lithnet.AccessManager.Agent.Providers;
using System;

namespace Lithnet.AccessManager.Agent
{
    public class RandomPasswordGenerator : IPasswordGenerator
    {
        private readonly ISettingsProvider settings;
        private readonly IRandomValueGenerator randomValueGenerator;

        public RandomPasswordGenerator(ISettingsProvider settings, IRandomValueGenerator rvg)
        {
            this.settings = settings;
            this.randomValueGenerator = rvg;
        }

        public string Generate()
        {
            if (string.IsNullOrWhiteSpace(this.settings.PasswordCharacters))
            {
                return this.randomValueGenerator.GenerateRandomString(Math.Max(this.settings.PasswordLength, 8), this.settings.UseLower, this.settings.UseUpper, this.settings.UseNumeric, this.settings.UseSymbol);
            }
            else
            {
                return this.randomValueGenerator.GenerateRandomString(Math.Max(this.settings.PasswordLength, 8), this.settings.PasswordCharacters);

            }
        }
    }
}
