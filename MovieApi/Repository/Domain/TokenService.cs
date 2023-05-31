using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using MovieApi.Models.DTO;
using MovieApi.Repository.Abstract;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MovieApi.Repository.Domain
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        public TokenService(IConfiguration configuration) 
        { 
            _configuration = configuration;
        }
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
                ValidateIssuer = false,
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidateAudience = false,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidateLifetime = false // Allow expired tokens
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            return principal;
        }

        public string GetRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public TokenResponse GetToken(IEnumerable<Claim> claim)
        {
            var authSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claim,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return new TokenResponse { TokenString = tokenString, ValidTo = token.ValidTo };  
        }
    }
}
