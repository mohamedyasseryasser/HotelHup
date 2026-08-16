using HotelHup.APPLICATION.DTO.auth;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AppContext = HotelHup.INFRASTRUCTURE.Context.hotelhupContext;
using RefreshToken = HotelHup.CORE.Entities.RefreshToken;

namespace HotelHup.INFRASTRUCTURE.repos.implementation
{


    public sealed class AuthRepo : IAuthRepo
    {
        private readonly AppContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthRepo(AppContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public Task<User?> FindUserByEmailAsync(string email)
        {
            var result = _userManager.FindByEmailAsync(email);
            return result;
        }
        public Task<User?> FindUserByIdAsync(string userId) =>
                    _userManager.FindByIdAsync(userId);

        public Task<bool> CheckPasswordAsync(User user, string password) => _userManager.CheckPasswordAsync(user, password);

        public Task<IList<string>> GetRolesAsync(User user) => _userManager.GetRolesAsync(user);

        public async Task<ResponseStatus<bool>>ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            var response = new ResponseStatus<bool>();
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                response.Success = true;
                response.Data = true;
                response.Message = "Password changed successfully.";
                return response;
            }
            else
            {
                response.Success = false;
                response.Errors=result.Errors.Select(e => e.Description).ToList();
                return response;
            }
        }

        public async Task<ResponseStatus<bool>> ChangeRoleAsync(User user, string role)
        {
            var response = new ResponseStatus<bool>();
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    response.Success = false;
                    response.Errors.AddRange(roleResult.Errors.Select(e => e.Description));
                    return response;
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                response.Success = false;
                response.Errors.AddRange(removeResult.Errors.Select(e => e.Description));
                return response;
            }
            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (addResult.Succeeded)
            {
                response.Success = true;
                response.Data = true;
                return response;
            }
            response.Success = false;
            response.Errors.AddRange(addResult.Errors.Select(e => e.Description));  
            return response;
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return _context.RefreshTokens.Include(x => x.User).
            SingleOrDefaultAsync(x => x.Token.ToLower() == token.ToLower());
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public Task RevokeRefreshTokenAsync(RefreshToken refreshToken, string? replacedByToken = null)
        {
            refreshToken.IsRevoked = true;
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public TokenResponseDto CreateToken(User user, IList<string> roles)
        {
            var now = DateTime.UtcNow;
            var accessLifetime = TimeSpan.FromHours(_configuration.GetValue<double?>("JWT:DurationInHours") ?? 1);
            var refreshLifetime = TimeSpan.FromDays(_configuration.GetValue<double?>("JWT:RefreshTokenDurationInDays") ?? 7);
            var accessExpiresAt = now.Add(accessLifetime);
            var refreshExpiresAt = now.Add(refreshLifetime);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key is missing.")));
            var email = user.Email ?? string.Empty;
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, user.FullName ?? user.UserName ?? email)
        };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var jwt = new JwtSecurityToken(_configuration["JWT:Issuer"], _configuration["JWT:Audience"], claims, now, accessExpiresAt, new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new TokenResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(jwt),
                RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                AccessTokenExpiresAt = accessExpiresAt,
                RefreshTokenExpiresAt = refreshExpiresAt,
                UserId = user.Id,
                UserName = email,
                Roles = roles
            };
        }
        public async Task<TokenResponseDto> IssueTokenAsync(User user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var roles = await GetRolesAsync(user);
                var responsetoken = CreateToken(user, roles);

                await AddRefreshTokenAsync(new RefreshToken
                {
                    Token = responsetoken.RefreshToken,
                    UserId = user.Id,
                    User = user,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = responsetoken.RefreshTokenExpiresAt
                });

                await SaveChangesAsync();
                await transaction.CommitAsync();
                return responsetoken;

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
