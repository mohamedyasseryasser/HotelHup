using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IAuthRepository
    {
        Task<User?> GetUserForLogoutAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<User?> GetUserForLoginAsync(
            string normalizedUserName,
            CancellationToken cancellationToken = default);

        Task<RefreshToken?> GetRefreshTokenAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task PersistRefreshTokenAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default);

        Task AddAuthenticationAuditAsync(
            User user,
            string action,
            CancellationToken cancellationToken = default);

        Task<bool> RotateRefreshTokenAsync(
            int currentTokenId,
            RefreshToken replacementToken,
            CancellationToken cancellationToken = default);

        Task<bool> RevokeRefreshTokensAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
