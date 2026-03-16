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
       
        public List<string> CheckToken(string token)
        {
            try
            {
                var secretKey = configuration["SecretKey"];
                var handler = new JwtSecurityTokenHandler();

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

                var userScopes = principal.Claims
                    .Where(c => c.Type == "scope")
                    .Select(c => c.Value);

                return userScopes.ToList();
            }
            catch
            {
                return new List<string> { "Invalid Token" };
            }
        }
    }
}
