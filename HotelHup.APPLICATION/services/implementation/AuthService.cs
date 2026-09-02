using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Auth;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Identity;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class AuthService : IAuthService
    {
        private const string InvalidCredentialsMessage = "Invalid username or password.";
        private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";
        private readonly IAuthRepository _repository;
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;

        public AuthService(
            IAuthRepository repository,
            ITokenService tokenService,
            UserManager<User> userManager)
        {
            _repository = repository;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        public async Task<ResponseStatus<LoginResponseDto>> LoginAsync(
            LoginRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null )
            {
                return Failure<LoginResponseDto>(InvalidCredentialsMessage, 401);
            }

            var normalizedUserName = _userManager.NormalizeName(request.UserName.Trim());
            var user = await _repository.GetUserForLoginAsync(normalizedUserName, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return Failure<LoginResponseDto>(InvalidCredentialsMessage, 401);
            }

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            if (lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return Failure<LoginResponseDto>(InvalidCredentialsMessage, 401);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                if (_userManager.SupportsUserLockout && await _userManager.GetLockoutEnabledAsync(user))
                {
                    await _userManager.AccessFailedAsync(user);
                }

                return Failure<LoginResponseDto>(InvalidCredentialsMessage, 401);
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            var roles = (await _userManager.GetRolesAsync(user)).ToArray();
            var tokenPairs = await _tokenService.CreateTokenPairAsync(user, roles, cancellationToken);
            await _repository.PersistRefreshTokenAsync(tokenPairs.RefreshTokenEntity, cancellationToken);
            await _repository.AddAuthenticationAuditAsync(user, "Login Success", cancellationToken);

            return new ResponseStatus<LoginResponseDto>(
                ToResponse(tokenPair, user, roles),
                "Login successful.",
                200);
        }

        public async Task<ResponseStatus<LoginResponseDto>> RefreshAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Failure<LoginResponseDto>(InvalidRefreshTokenMessage, 401);
            }

            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var storedToken = await _repository.GetRefreshTokenAsync(tokenHash, cancellationToken);
            if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow || storedToken.User is null || !storedToken.User.IsActive)
            {
                return Failure<LoginResponseDto>(InvalidRefreshTokenMessage, 401);
            }

            var roles = (await _userManager.GetRolesAsync(storedToken.User)).ToArray();
            var replacement = await _tokenService.CreateTokenPairAsync(storedToken.User, roles, cancellationToken);
            var rotated = await _repository.RotateRefreshTokenAsync(
                storedToken.Id,
                replacement.RefreshTokenEntity,
                cancellationToken);

            if (!rotated)
            {
                return Failure<LoginResponseDto>(InvalidRefreshTokenMessage, 401);
            }

            await _repository.AddAuthenticationAuditAsync(storedToken.User, "Refresh", cancellationToken);

            return new ResponseStatus<LoginResponseDto>(
                ToResponse(replacement, storedToken.User, roles),
                "Token refreshed successfully.",
                200);
        }

        public async Task<ResponseStatus<bool>> LogoutAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ResponseStatus<bool>("Authenticated user could not be identified.", statusCode: 401);
            }

            var user = await _repository.GetUserForLogoutAsync(userId, cancellationToken);
            if (user is null || !user.IsActive)
            {
                return new ResponseStatus<bool>("Authenticated user is invalid.", statusCode: 401);
            }

            await _repository.RevokeRefreshTokensAsync(user.Id, cancellationToken);
            return new ResponseStatus<bool>(true, "Logout successful.", 200);
        }

        private static LoginResponseDto ToResponse(
            TokenPair tokenPair,
            User user,
            IReadOnlyCollection<string> roles)
        {
            return new LoginResponseDto
            {
                AccessToken = tokenPair.AccessToken,
                RefreshToken = tokenPair.RefreshToken,
                AccessTokenExpiresAtUtc = tokenPair.AccessTokenExpiresAtUtc,
                User = new ResponseUserDto
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Address = user.Address,
                    PropertyId = user.PropertyId,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = roles.Select(role => new ResponseRoleDto { RoleName = role, isactive = true }).ToArray()
                }
            };
        }

        private static ResponseStatus<T> Failure<T>(string message, int statusCode)
        {
            return new ResponseStatus<T>(message, statusCode: statusCode);
        }
    }
}
