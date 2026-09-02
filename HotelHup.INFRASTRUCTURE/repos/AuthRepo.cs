using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.INFRASTRUCTURE.repos
{
    public sealed class AuthRepository : IAuthRepository
    {
        private readonly hotelhupContext _context;

        public AuthRepository(hotelhupContext context)
        {
            _context = context;
        }

        public Task<User?> GetUserForLogoutAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        }

        public Task<User?> GetUserForLoginAsync(
            string normalizedUserName,
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .SingleOrDefaultAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken);
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return _context.Set<RefreshToken>()
                .Include(token => token.User)
                .SingleOrDefaultAsync(token => token.Token == tokenHash, cancellationToken);
        }

        public async Task PersistRefreshTokenAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            _context.Set<RefreshToken>().Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddAuthenticationAuditAsync(
            User user,
            string action,
            CancellationToken cancellationToken = default)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                PropertyId = user.PropertyId,
                EntityName = nameof(User),
                EntityId = user.Id,
                Action = action,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> RotateRefreshTokenAsync(
            int currentTokenId,
            RefreshToken replacementToken,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var revokedCount = await _context.Set<RefreshToken>()
                .Where(token => token.Id == currentTokenId && !token.IsRevoked)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.IsRevoked, true),
                    cancellationToken);

            if (revokedCount != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _context.Set<RefreshToken>().Add(replacementToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RevokeRefreshTokensAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var activeTokens = await _context.Set<RefreshToken>()
                .Where(token => token.UserId == userId && !token.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }

            var user = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

            if (user is not null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    PropertyId = user.PropertyId,
                    EntityName = nameof(User),
                    EntityId = user.Id,
                    Action = "Logout",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            if (activeTokens.Count > 0 || user is not null)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
