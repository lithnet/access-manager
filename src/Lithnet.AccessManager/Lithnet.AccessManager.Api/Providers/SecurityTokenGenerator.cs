using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;


namespace Lithnet.AccessManager.Api.Providers
{
    public class SecurityTokenGenerator : ISecurityTokenGenerator
    {
        public string GenerateToken(string subject, IEnumerable<Claim> claims)
        {
            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupers3cr3tsharedkey!"));

            string myIssuer = "https://{yourOktaDomain}/oauth2/default";
            string myAudience = "api://default";

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, subject) }),
                Claims = claims.ToDictionary<Claim, string, object>(t => t.Type, u => u),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(sharedKey, SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateToken(ClaimsIdentity identity)
        {
            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupers3cr3tsharedkey!"));

            string myIssuer = "https://{yourOktaDomain}/oauth2/default";
            string myAudience = "api://default";

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(sharedKey, SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateToken(IList<Claim> claims)
        {
            SymmetricSecurityKey sharedKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mysupers3cr3tsharedkey!"));

            string myIssuer = "https://{yourOktaDomain}/oauth2/default";
            string myAudience = "api://default";

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken tokenDescriptor = new JwtSecurityToken
            (
                myIssuer, 
                myAudience,
                claims,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                new SigningCredentials(sharedKey, SecurityAlgorithms.HmacSha256)
            );

           // var tokenDescriptor = new SecurityTokenDescriptor
            //{
            //    Subject = principal.Identity as ClaimsIdentity,
            //    Expires = DateTime.UtcNow.AddHours(1),
            //    Issuer = myIssuer,
            //    Audience = myAudience,
            //    SigningCredentials = new SigningCredentials(sharedKey, SecurityAlgorithms.HmacSha256Signature)
            //};

            //var token = tokenHandler.CreateToken(tokenDescriptor);
            string token = tokenHandler.WriteToken(tokenDescriptor);

            return token;
        }
    }
}
