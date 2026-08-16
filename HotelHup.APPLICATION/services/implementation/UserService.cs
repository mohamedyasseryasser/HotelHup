using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
namespace HotelHup.APPLICATION.services.implementation
{
    public class UserService : IUserService
    {
        private readonly IUserRepo _userRepo;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepo userRepo, ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<ResponseStatus<responseuserdto>> AddAsync(AddUserDto dto, UserRole role)
        {
            var response = new ResponseStatus<responseuserdto>();
            try
            {
                bool emailExists = await _userRepo.CheckExistEmailAsync(dto.Email);
                if (emailExists)
                {
                    response.Success = false;
                    response.Message = "Email already exists";
                    return response;
                }

                var userEntity = new User
                {
                    UserName = dto.UserName,
                    Email = dto.Email,
                    FullName = dto.FullName,
                    Address = dto.Address,
                    PhoneNumber = dto.PhoneNumber,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                var dbDto = new responseuserdatabasedto
                {
                    user = userEntity,
                    password = dto.Password
                };

                var createdUser = await _userRepo.AddAsync(dbDto, role);
                await _userRepo.SaveChangesAsync();
                response.Success = true;
                response.Message = "User created successfully";
                response.Data = new responseuserdto
                {
                    UserName = createdUser.UserName,
                    Email = createdUser.Email,
                    FullName = createdUser.FullName,
                    Address = createdUser.Address,
                    PhoneNumber = createdUser.PhoneNumber,
                    IsActive = createdUser.IsActive,
                    role = role.ToString()
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding user with email {Email}", dto.Email);
                response.Success = false;
                response.Message = "An unexpected error occurred while processing your request.";
                response.Errors.Add(ex.Message);
                return response;
            }
        }

        public async Task<ResponseStatus<responseuserdto>> UpdateAsync(UpdateUserDto dto, UserRole role)
        {
            var response = new ResponseStatus<responseuserdto>();
            try
            {
                var user = await _userRepo.GetByIdAsync(dto.userid, role);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    return response;
                }
                if (dto.FullName != null) user.FullName = dto.FullName;
                if (dto.Address != null) user.Address = dto.Address;
                if (dto.Email != null) user.Email = dto.Email;
                if (dto.UserName != null) user.UserName = dto.UserName;
                if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
                if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

                var data = new responseuserdatabasedto { user = user };
                var updatedUser = await _userRepo.UpdateAsync(data, role);
                await _userRepo.SaveChangesAsync();
                response.Success = true;
                response.Message = "User updated successfully";
                response.Data = new responseuserdto
                {
                    UserName = updatedUser.UserName,
                    Email = updatedUser.Email,
                    FullName = updatedUser.FullName,
                    Address = updatedUser.Address,
                    PhoneNumber = updatedUser.PhoneNumber,
                    IsActive = updatedUser.IsActive,
                    role = role.ToString()
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user with ID {UserId}", dto.userid);
                response.Success = false;
                response.Message = ex.Message;
                response.Errors.Add(ex.ToString());
                return response;
            }
        }

        public async Task<ResponseStatus<responseuserdto>> GetByIdAsync(string userId, UserRole role)
        {
            var response = new ResponseStatus<responseuserdto>();
            try
            {
                var user = await _userRepo.GetByIdAsync(userId, role);
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found";
                    return response;
                }

                response.Success = true;
                response.Data = new responseuserdto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    role = role.ToString()
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by ID {UserId}", userId);
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        public async Task<ResponseStatus<IEnumerable<responseuserdto>>> GetAllAsync(pagination pg, UserRole role, string? fullname = null, string? email = null)
        {
            var response = new ResponseStatus<IEnumerable<responseuserdto>>();
            try
            {
                var users = await _userRepo.GetAllAsync(pg, role, fullname, email);
                response.Success = true;
                response.Data = users.Select(u => new responseuserdto
                {
                    UserName = u.UserName,
                    Email = u.Email,
                    FullName = u.FullName,
                    Address = u.Address,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    role = role.ToString()
                }).ToList();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users for role {Role}", role);
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        public async Task<ResponseStatus<responseuserdto>> RemoveAsync(string id, UserRole role)
        {
            var response = new ResponseStatus<responseuserdto>();
            try
            {
                var user = await _userRepo.RemoveAsync(id, role);
                await _userRepo.SaveChangesAsync();
                response.Success = true;
                response.Message = "User deactivated successfully";
                response.Data = new responseuserdto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    role = role.ToString()
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing user with ID {UserId}", id);
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        public async Task<ResponseStatus<int>> GetCountAsync(UserRole role)
        {
            var response = new ResponseStatus<int>();
            try
            {
                var count = await _userRepo.GetCountAsync(role);
                response.Success = true;
                response.Data = count;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting count for role {Role}", role);
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        public async Task<ResponseStatus<IEnumerable<responseuserdto>>> GetAllActiveAsync(UserRole role)
        {
            var response = new ResponseStatus<IEnumerable<responseuserdto>>();
            try
            {
                var users = await _userRepo.GetAllActiveAsync(role);
                response.Success = true;
                response.Data = users.Select(u => new responseuserdto
                {
                    UserName = u.UserName,
                    Email = u.Email,
                    FullName = u.FullName,
                    Address = u.Address,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    role = role.ToString()
                }).ToList();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active users for role {Role}", role);
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }
    }
}

