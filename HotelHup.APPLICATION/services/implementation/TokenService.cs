using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HotelHup.INFRASTRUCTURE.services
{
    public sealed class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<TokenPair> CreateTokenPairAsync(
            User user,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var key = _configuration["JWT:Key"]
                ?? throw new InvalidOperationException("JWT:Key not found.");
            var issuer = _configuration["JWT:Issuer"];
            var audience = _configuration["JWT:Audience"];
            var durationInHours = _configuration.GetValue<int?>("JWT:DurationInHours") ?? 1;
            var refreshDurationInDays = _configuration.GetValue<int?>("JWT:RefreshTokenDurationInDays") ?? 7;
            var now = DateTime.UtcNow;
            var accessExpiresAtUtc = now.AddHours(durationInHours);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            if (user.PropertyId.HasValue)
            {
                claims.Add(new Claim("PropertyId", user.PropertyId.Value.ToString()));
            }

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                notBefore: now,
                expires: accessExpiresAtUtc,
                signingCredentials: credentials);

            var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenEntity = new RefreshToken
            {
                Token = HashRefreshToken(rawRefreshToken),
                CreatedAt = now,
                ExpiresAt = now.AddDays(refreshDurationInDays),
                IsRevoked = false,
                UserId = user.Id
            };

            return Task.FromResult(new TokenPair
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                RefreshToken = rawRefreshToken,
                AccessTokenExpiresAtUtc = accessExpiresAtUtc,
                RefreshTokenEntity = refreshTokenEntity
            });
        }

        public string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }
    }
}
