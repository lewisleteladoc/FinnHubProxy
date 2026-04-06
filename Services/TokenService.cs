using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinnHubProxy.Services
{
    public class TokenService
    {
        private readonly IConfiguration configuration;

        public TokenService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
       
        public List<string> CheckToken(string token, string type="")
        {
            try
            {
                var secretKey = configuration["SecretKey"];
                var handler = new JwtSecurityTokenHandler();
                handler.InboundClaimTypeMap.Clear(); // 

                var principal = handler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = "VM02_DA_NANG",
                        ValidAudience = "MyClient",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Convert.FromBase64String(secretKey))
                    },
                    out SecurityToken validatedToken
                );

                // 1. Get the 'sub' (Subject)
                // Note: Use "sub" or ClaimTypes.NameIdentifier depending on your mapping
                var sub = principal.Claims.FirstOrDefault(c => c.Type == "sub" && type == "sub")?.Value;

                // 2. Get the scopes
                var userScopes = principal.Claims
                    .Where(c => c.Type == "scope")
                    .Select(c => c.Value)
                    .ToList();

                // 3. Add 'sub' to the list
                if (sub != null) {
                    return new List<string> { sub };
                } else
                {
                    return userScopes;
                }                    
            }
            catch
            {
                return new List<string> { "Invalid Token" };
            }
        }
    }
}
