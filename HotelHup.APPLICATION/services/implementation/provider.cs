using System.Security.Cryptography;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class SystemActorProvider : ISystemActorProvider
{
    private const string DefaultUserName = "system@hotelhup.local";
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemActorProvider> _logger;

    public SystemActorProvider(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration,
        ILogger<SystemActorProvider> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<User?> GetAsync(CancellationToken ct = default)
    {
        var userName = _configuration["Hangfire:SystemUserName"]?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = DefaultUserName;
        }

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new User
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = userName,
                NormalizedEmail = userName.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = "HotelHup Background System",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var password = $"Aa1!{Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))}";
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Unable to create Hangfire system actor {SystemUserName}: {Errors}",
                    userName,
                    string.Join("; ", createResult.Errors.Select(x => x.Code)));
                return null;
            }
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Unable to activate Hangfire system actor {SystemUserName}.", userName);
                return null;
            }
        }

        var roleName = UserRole.Admin.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await _roleManager.CreateAsync(new Role
            {
                Name = roleName,
                IsActive = true,
                Description = "Internal HotelHup system role."
            });
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Unable to create system actor role {RoleName}.", roleName);
                return null;
            }
        }

        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            _logger.LogError("Unable to resolve system actor role {RoleName}.", roleName);
            return null;
        }

        if (!role.IsActive)
        {
            role.IsActive = true;
            var roleUpdateResult = await _roleManager.UpdateAsync(role);
            if (!roleUpdateResult.Succeeded)
            {
                _logger.LogError("Unable to activate system actor role {RoleName}.", roleName);
                return null;
            }
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Unable to assign {RoleName} to Hangfire system actor {SystemUserName}.", roleName, userName);
                return null;
            }
        }

        return user;
    }
}
