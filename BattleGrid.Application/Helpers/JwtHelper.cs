using BattleGrid.Application.Interfaces;
using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BattleGrid.Application.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _config;
        private readonly IUserServices _userServices;

        public JwtHelper(IConfiguration config, IUserServices userServices)
        {
            _config = config;
            _userServices = userServices;
        }

        // Generate a new access token with JWT for players
        public async Task<string> GenerateAccessToken(string loginInfo)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // We will be calling this helper only when the customer exists and logs in successfully. So, no need fo a null check
            var user = await _userServices.GetByLoginInfoAsync(loginInfo);

            if (user is null)
            {
                throw new NullReferenceException("There is no such user to assign access permissions!");
            }

            bool isBanned = user.IsBanned;

            if (isBanned)
            {
                throw new SecurityException("Can not provide access to banned players!");
            }

            string role = user.IsAdmin ? "Admin" : "Player";

            var claims = new[]
            {
                new Claim("userId", user.UserID.ToString()),
                // This can be null an cause problems. We should implement that random and unique UserNameGenerator
                //new Claim(ClaimTypes.UserData, "UserName", user.UserName.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("user role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: GetAccessTokenExpiration().UtcDateTime,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Generate a random refresh token encoded in base64
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Get the principal from an expired token (used to refresh the access token)
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSection = _config.GetSection("Jwt");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
                ValidateLifetime = false, // We don't care about the token's expiration date in jwt system. We are using an expired token
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        // Add 15 minutes to the current time to get the access token expiration
        public DateTimeOffset GetAccessTokenExpiration()
        {
            var jwtSection = _config.GetSection("Jwt");
            return DateTimeOffset.Now.AddMinutes(double.Parse(jwtSection["AccessTokenMinutes"] ?? "15")).ToUniversalTime();
        }

        // Add 7 days to the current time to get the refresh token expiration
        public DateTimeOffset GetRefreshTokenExpiration()
        {
            var jwtSection = _config.GetSection("Jwt");
            return DateTimeOffset.Now.AddDays(double.Parse(jwtSection["RefreshTokenDays"] ?? "7")).ToUniversalTime();
        }

        // Get the principal from a token with lifetime validation
        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var jwtSection = _config.GetSection("Jwt");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
                ValidateLifetime = true, // Validate token expiration
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}