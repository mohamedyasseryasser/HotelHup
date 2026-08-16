using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.auth;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.Extensions.Logging;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class AuthService : IAuthService
    {
        private readonly IAuthRepo _authRepo;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAuthRepo authRepo, ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
            _logger = logger;
        }

        public async Task<ResponseStatus<TokenResponseDto>> LoginAsync(LoginDTO request)
        {
            try
            {
                var user = await _authRepo.FindUserByEmailAsync(request.Email);

                if (user is null)
                    return Failure<TokenResponseDto>("User was not found.");

                if (!user.IsActive)
                    return Failure<TokenResponseDto>("User is not active.");

                var passwordIsValid = await _authRepo.CheckPasswordAsync(user, request.Password);
                if (!passwordIsValid)
                    return Failure<TokenResponseDto>("The password is invalid.");

                var token = await _authRepo.IssueTokenAsync(user);
                return Success(token, "Login was successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                return Failure<TokenResponseDto>("An error occurred during login. Please try again.");
            }
        }

        public async Task<ResponseStatus<bool>> LogoutAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return Failure<bool>("Refresh token is required.");

                var storedToken = await _authRepo.GetRefreshTokenAsync(refreshToken);
                if (storedToken is null)
                    return Failure<bool>("Refresh token was not found.");

                if (!storedToken.IsRevoked)
                    await _authRepo.RevokeRefreshTokenAsync(storedToken);

                await _authRepo.SaveChangesAsync();
                return Success(true, "Logout was successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during logout.");
                return Failure<bool>("An error occurred during logout. Please try again.");
            }
        }

        public async Task<ResponseStatus<TokenResponseDto>> RefreshTokenAsync(RefreshTokenDto request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Failure<TokenResponseDto>("Refresh token is required.");

                var currentToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);

                if (currentToken is null)
                    return Failure<TokenResponseDto>("Refresh token was not found.");

                if (currentToken.IsRevoked)
                    return Failure<TokenResponseDto>("Refresh token has been revoked.");

                if (currentToken.ExpiresAt <= DateTime.UtcNow)
                    return Failure<TokenResponseDto>("Refresh token has expired.");

                if (currentToken.User is null || !currentToken.User.IsActive)
                    return Failure<TokenResponseDto>("The user associated with this refresh token is not active.");

                var newToken = await _authRepo.IssueTokenAsync(currentToken.User);
                await _authRepo.RevokeRefreshTokenAsync(currentToken, newToken.RefreshToken);
                await _authRepo.SaveChangesAsync();

                return Success(newToken, "Refresh token was renewed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while refreshing the token.");
                return Failure<TokenResponseDto>("An error occurred while refreshing the token. Please try again.");
            }
        }

        public async Task<ResponseStatus<TokenResponseDto>> CreateTokenAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Failure<TokenResponseDto>("User ID is required.");

                var user = await _authRepo.FindUserByIdAsync(userId);
                if (user is null)
                    return Failure<TokenResponseDto>("User was not found.");

                if (!user.IsActive)
                    return Failure<TokenResponseDto>("User is not active.");

                var token = await _authRepo.IssueTokenAsync(user);
                return Success(token, "Token was created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the token.");
                return Failure<TokenResponseDto>("An error occurred while creating the token. Please try again.");
            }
        }

        public async Task<ResponseStatus<bool>> ChangeRoleAsync(ChangeRoleDTO request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.UserId))
                    return Failure<bool>("User ID is required.");

                if (string.IsNullOrWhiteSpace(request.Role))
                    return Failure<bool>("Role is required.");

                var user = await _authRepo.FindUserByIdAsync(request.UserId);
                if (user is null)
                    return Failure<bool>("User was not found.");

                var result = await _authRepo.ChangeRoleAsync(user, request.Role.Trim());
                if (!result.Success)
                {
                    return Failure<bool>("The user role could not be changed.", result.Errors.ToList());
                }
                await _authRepo.SaveChangesAsync();
                return Success(true, "User role was changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing the user role.");
                return Failure<bool>("An error occurred while changing the user role. Please try again.");
            }
        }

        public async Task<ResponseStatus<bool>> ChangePasswordAsync(ChangePasswordDTO request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.Email))
                    return Failure<bool>("Email is required.");

                if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                    return Failure<bool>("Current password and new password are required.");

                var user = await _authRepo.FindUserByEmailAsync(request.Email);
                if (user is null)
                    return Failure<bool>("User was not found.");

                if (!user.IsActive)
                    return Failure<bool>("User is not active.");

                var result = await _authRepo.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Success)
                    return Failure<bool>("The password could not be changed.", result.Errors.ToList());

                return Success(true, "Password was changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing the password.");
                return Failure<bool>("An error occurred while changing the password. Please try again.");
            }
        }

        private static ResponseStatus<T> Success<T>(T data, string message) =>
            new(data, message, true);

        private static ResponseStatus<T> Failure<T>(string message, List<string>? errors = null) =>
            new(default!, message, false, errors ?? new List<string> { message });
    }
}
