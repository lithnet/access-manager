using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Models
{
    public class TokenResponse
    {
        public TokenResponse()
        {
        }

        public TokenResponse(string token, string type, DateTime expiry)
        {
            this.Token = token;
            this.TokenType = type;
            this.ExpiryDate = expiry;
        }

        public TokenResponse(string token, DateTime expiry)
        {
            this.Token = token;
            this.ExpiryDate = expiry;
        }

        [JsonIgnore] public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("access_token")]
        public string Token { get; set; }

        [JsonPropertyName("token_type")] public string TokenType { get; set; } = "bearer";

        [JsonPropertyName("expires_in")] public int? ExpiresIn => (int?)this.ExpiryDate?.Subtract(DateTime.UtcNow).TotalSeconds;
    }
}
