using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IUserRepo
    {
        Task<User> AddAsync(responseuserdatabasedto user, UserRole role);

        Task<User> UpdateAsync(responseuserdatabasedto dto, UserRole role);

        Task<User?> GetByIdAsync(string userId, UserRole role);

        Task<IEnumerable<User>> GetAllAsync(pagination pg, UserRole role, string? fullname = null, string? email = null);

        Task<User> RemoveAsync(string id, UserRole role);

        Task<int> GetCountAsync(UserRole role);

        Task<IEnumerable<User>> GetAllActiveAsync(UserRole role);

        Task SaveChangesAsync();

        Task<bool> CheckExistEmailAsync(string email);
    }
}
