using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IUserService
    {
        Task<ResponseStatus<bool>> checkpermissionandstateuser(string userid,CancellationToken cancellationToken,string permission);
        Task<ResponseStatus<PagedResponse<ResponseUserDto>>> GetListAsync(User actor,UserListRequestDto request, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> GetByIdAsync(User currentuser,string userId, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> CreateAsync(User user,CreateUserRequestDto request, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> UpdateAsync(User actor,string userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> ActivateAsync(User currentuser,string userId, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> DeactivateAsync(User currentuser,string userId, CancellationToken cancellationToken = default);
        Task<ResponseStatus<IReadOnlyList<ResponseRoleDto>>> GetRolesAsync(User currentuser,string userId, CancellationToken cancellationToken = default);
        Task<ResponseStatus<ResponseUserDto>> ReplaceRolesAsync(User actor,string userId, ReplaceUserRolesRequestDto request, CancellationToken cancellationToken = default);
    }
}
