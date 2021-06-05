using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Api
{
    public class SignedAssertionValidationOptions
    {
        public int MaximumAssertionValidityMinutes { get; set; } = 5;

        public List<string> AllowedSigningAlgorithms { get; set; } = new List<string>
        {
            SecurityAlgorithms.RsaSha256,
            SecurityAlgorithms.RsaSha384,
            SecurityAlgorithms.RsaSha512,
            SecurityAlgorithms.RsaSsaPssSha256,
            SecurityAlgorithms.RsaSsaPssSha384,
            SecurityAlgorithms.RsaSsaPssSha512
        };
    }
}
