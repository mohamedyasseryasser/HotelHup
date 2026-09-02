using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Auth;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IAuthService
    {
        Task<ResponseStatus<LoginResponseDto>> LoginAsync(
            LoginRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ResponseStatus<LoginResponseDto>> RefreshAsync(
            RefreshTokenRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ResponseStatus<bool>> LogoutAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
