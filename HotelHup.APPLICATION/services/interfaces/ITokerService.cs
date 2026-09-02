using HotelHup.APPLICATION.DTO.Auth;
using HotelHup.CORE.Entities;
using System.Security.Claims;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface ITokenService
    {
        Task<TokenPair> CreateTokenPairAsync(
            User user,
            IReadOnlyCollection<string> roles,
            CancellationToken cancellationToken = default);

        string HashRefreshToken(string refreshToken);
    }

    public sealed class TokenPair
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; init; }
        public RefreshToken RefreshTokenEntity { get; init; } = null!;
    }
}
