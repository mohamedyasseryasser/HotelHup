using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IUserService
    {
        Task<ResponseStatus<responseuserdto>> AddAsync(
       AddUserDto dto,
       UserRole role);

        Task<ResponseStatus<responseuserdto>> UpdateAsync(
            UpdateUserDto dto,
            UserRole role);

        Task<ResponseStatus<responseuserdto>> GetByIdAsync(
            string userId,
            UserRole role);

        Task<ResponseStatus<IEnumerable<responseuserdto>>> GetAllAsync(
            pagination pg,
            UserRole role,
            string? fullname = null,
            string? email = null);

        Task<ResponseStatus<responseuserdto>> RemoveAsync(
            string id,
            UserRole role);

        Task<ResponseStatus<int>> GetCountAsync(
            UserRole role);

        Task<ResponseStatus<IEnumerable<responseuserdto>>> GetAllActiveAsync(
            UserRole role);
    }
}
