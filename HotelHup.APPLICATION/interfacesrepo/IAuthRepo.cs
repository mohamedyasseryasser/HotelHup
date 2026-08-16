using HotelHup.APPLICATION.DTO.auth;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IAuthRepo
    {
        Task<User?> FindUserByEmailAsync(string email);
        Task<User?> FindUserByIdAsync(string userId);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<IList<string>> GetRolesAsync(User user);
        Task<ResponseStatus<bool>> ChangePasswordAsync(User user, string currentPassword, string newPassword);
        Task<ResponseStatus<bool>> ChangeRoleAsync(User user, string role);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string? replacedByToken = null);
        Task SaveChangesAsync();
        TokenResponseDto CreateToken(User user, IList<string> roles);
        Task<TokenResponseDto> IssueTokenAsync(User user);

    }
}