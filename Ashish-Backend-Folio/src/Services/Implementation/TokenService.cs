using Ashish_Backend_Folio.Interfaces;
using Ashish_Backend_Folio.Models;
using Ashish_Backend_Folio.Storage.Interface;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Ashish_Backend_Folio.Services.Implementation
{
    public class TokenService : ITokenService
    {

        private readonly IConfiguration _config;
        private readonly ISecretProvider _secretProvider;


        public TokenService(IConfiguration config,
            ISecretProvider secretProvider)
        {
            _config = config;
            _secretProvider = secretProvider;
        }

        public async Task<string> CreateAccessTokenAsync(ApplicationUser user, IList<string> roles)
        {
            
                var jwt = _config.GetSection("Jwt");
                var jwtSecret = jwt["SigningKeySecretName"];
                if (string.IsNullOrEmpty(jwtSecret)) throw new Exception("jwt sign key not found");

                var keySecret = await _secretProvider.GetSecretAsync(jwtSecret);
                if (string.IsNullOrEmpty(keySecret)) throw new Exception("jwt sign secret key not found");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keySecret));


                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, user.Id),
                    new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                    new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // add unique ID
                };

                // roles
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = new JwtSecurityToken(
                    issuer: jwt["Issuer"],
                    audience: jwt["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiresMinutes"])),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
