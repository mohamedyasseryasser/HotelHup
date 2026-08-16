using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppContext = HotelHup.INFRASTRUCTURE.Context.hotelhupContext;

namespace HotelHup.INFRASTRUCTURE.repos.implementation
{
    public class UserRepo : IUserRepo
    {
        public AppContext Context { get; }
        public UserManager<User> UserManager { get; }
        public RoleManager<IdentityRole> RoleManager { get; }

        public UserRepo(AppContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            Context = context;
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public async Task<bool> CheckExistEmailAsync(string email)
        {
            var result = await UserManager.FindByEmailAsync(email);
            return result != null;
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await RoleManager.RoleExistsAsync(roleName))
            {
                await RoleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public async Task<User> AddAsync(responseuserdatabasedto data, UserRole role)
        {
            using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                var result = await UserManager.CreateAsync(data.user, data.password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user: {errors}");
                }

                string roleName = role.ToString();
                await EnsureRoleExistsAsync(roleName);

                var roleResult = await UserManager.AddToRoleAsync(data.user, roleName);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException($"Failed to assign role: {errors}");
                }

                await transaction.CommitAsync();
                return data.user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<User> UpdateAsync(responseuserdatabasedto dto, UserRole role)
        {
            using var transaction = await Context.Database.BeginTransactionAsync();
            try
            {
                var user = await UserManager.FindByIdAsync(dto.user.Id);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found");
                }

                var result = await UserManager.UpdateAsync(dto.user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update user: {errors}");
                }

                await transaction.CommitAsync();
                await Context.SaveChangesAsync();
                return user;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<User?> GetByIdAsync(string userId, UserRole role)
        {
            return await UserManager.FindByIdAsync(userId);
        }

        public async Task<IEnumerable<User>> GetAllAsync(pagination pg, UserRole role, string? fullname = null, string? email = null)
        {
            string roleName = role.ToString();
            var query = from u in Context.Users
                        join ur in Context.UserRoles on u.Id equals ur.UserId
                        join r in Context.Roles on ur.RoleId equals r.Id
                        where r.Name == roleName
                        select u;



            if (!string.IsNullOrEmpty(fullname))
            {
                query = query.Where(u => u.FullName.Contains(fullname));
            }
            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(u => u.Email.Contains(email));
            }

            int skip = Math.Max(0, pg.PageNumber) * pg.PageSize;
            return await query.Skip(skip).Take(pg.PageSize).ToListAsync();
        }

        public async Task<User> RemoveAsync(string id, UserRole role)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
            var roleName = role.ToString();
            if (!await UserManager.IsInRoleAsync(user, roleName))
            {
                throw new UnauthorizedAccessException(
                    $"User does not have the required role: {roleName}");
            }
            if (!user.IsActive)
            {
                throw new InvalidOperationException("User is already inactive");
            }
            user.IsActive = false;
            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to deactivate user: {errors}");
            }
            return user;
        }

        public async Task<int> GetCountAsync(UserRole role)
        {
            string roleName = role.ToString();
            return await (from u in Context.Users
                          join ur in Context.UserRoles on u.Id equals ur.UserId
                          join r in Context.Roles on ur.RoleId equals r.Id
                          where r.Name == roleName
                          select u).CountAsync();
        }

        public async Task<IEnumerable<User>> GetAllActiveAsync(UserRole role)
        {
            string roleName = role.ToString();
            var query =  from u in Context.Users 
                         join ur in Context.UserRoles on u.Id equals ur.UserId
                         join r in Context.Roles on ur.RoleId equals r.Id
                            where r.Name == roleName && u.IsActive
                            select u;

            return await query.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await Context.SaveChangesAsync();
        }
    }
}

