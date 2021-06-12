using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Shared
{ 
    public class TokenResponse
    {
        private int? expiresIn;

        public TokenResponse()
        {
        }

        public TokenResponse(string token, DateTime expiry)
            : this(token, "bearer", expiry)
        {
        }

        public TokenResponse(string token, string type, DateTime expiry)
        {
            this.Token = token;
            this.TokenType = type;
            this.ExpiryDate = expiry;
            this.expiresIn = (int?)this.ExpiryDate?.Subtract(DateTime.UtcNow).TotalSeconds;
        }

        [JsonIgnore]
        public DateTime? ExpiryDate { get; set; }

        [JsonPropertyName("access_token")]
        public string Token { get; set; }
        
        [JsonPropertyName("token_type")] 
        public string TokenType { get; set; } = "bearer";

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn
        {
            get => this.expiresIn;
            set
            {
                this.expiresIn = value;

                if (value == null)
                {
                    this.ExpiryDate = null;
                }
                else
                {
                    this.ExpiryDate = DateTime.UtcNow.AddSeconds(value.Value);
                }
            }
        }
    }
}