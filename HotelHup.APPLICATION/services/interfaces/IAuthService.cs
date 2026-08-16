using HotelHup.APPLICATION.DTO.auth;
using HotelHup.APPLICATION.DTO.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IAuthService
    {
        Task<ResponseStatus<TokenResponseDto>> LoginAsync(LoginDTO request);
        Task<ResponseStatus<bool>> LogoutAsync(string refreshToken);
        Task<ResponseStatus<TokenResponseDto>> RefreshTokenAsync(RefreshTokenDto request);
        Task<ResponseStatus<TokenResponseDto>> CreateTokenAsync(string userId);
        Task<ResponseStatus<bool>> ChangeRoleAsync(ChangeRoleDTO request);
        Task<ResponseStatus<bool>> ChangePasswordAsync(ChangePasswordDTO request);
    }
}
