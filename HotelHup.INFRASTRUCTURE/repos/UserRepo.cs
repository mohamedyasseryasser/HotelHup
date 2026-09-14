using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class UserRepository : IUserRepository
    {
        private readonly hotelhupContext _context;
        private readonly UserManager<User> _userManager;

        public UserRepository(hotelhupContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<User?> GetuserByIdAsync(string userId , CancellationToken cancellationToken = default)
        {
            var normalizedId = userId.Trim();
            return await
                 _context.Users.
                Include(u => u.Property).AsNoTracking().
                FirstOrDefaultAsync(u => u.Id == normalizedId  ,cancellationToken);
        }
        public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var normalizedId = userId.Trim();

            return await _context.Users
                .Include(user => user.Property)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user =>
                        user.Id == normalizedId && user.IsActive &&
                        _context.UserRoles.Any(ur =>
                            ur.UserId == user.Id &&
                            _context.Roles.Any(r =>
                                r.Id == ur.RoleId &&
                                r.IsActive)),
                    cancellationToken);
        }

        public Task<User?> GetByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            return _context.Users
                .Include(user => user.Property)
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken);
        }
 
        public async Task<IReadOnlyList<User>> GetListAsync(
            string? userName,
            string? fullName,
            bool? isActive,
            int? propertyId,
            string? roleId,
            DateTime? createdFrom,
            DateTime? createdTo,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = BuildUserQuery(userName, fullName, isActive, propertyId, roleId, createdFrom, createdTo);
            return await query
      .OrderByDescending(user => user.CreatedAt)
      .ThenBy(user => user.Id)
      .Skip(skip)
      .Take(take)
      .AsNoTracking()
      .ToListAsync(cancellationToken);
        }

        public Task<int> CountAsync(
            string? userName,
            string? fullName,
            bool? isActive,
            int? propertyId,
            string? roleId,
            DateTime? createdFrom,
            DateTime? createdTo,
            CancellationToken cancellationToken = default)
        {
            return BuildUserQuery(userName, fullName, isActive, propertyId, roleId, createdFrom, createdTo)
                .CountAsync(cancellationToken);
        }

        public Task<bool> ExistsByNormalizedUserNameAsync(
            string normalizedUserName,
            string? excludingUserId = null,
            CancellationToken cancellationToken = default)
        {
            return _context.Users.AnyAsync(
                user => user.NormalizedUserName == normalizedUserName &&
                        (excludingUserId == null || user.Id != excludingUserId),
                cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
        {

            return await _context.Set<Role>()
                .Where(role => _context.UserRoles.Any(userRole => userRole.UserId == userId && userRole.RoleId == role.Id))
                .OrderBy(role => role.Name)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(
            IEnumerable<string> roleIds,
            CancellationToken cancellationToken = default)
        {
            var ids =
                roleIds
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            return await _context
                .Set<Role>()
                .Where(role => ids.Contains(role.Id))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlySet<string>> GetPermissionNamesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var names = await (
                from userRole in _context.UserRoles
                join rolePermission in _context.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                join permission in _context.Permissions on rolePermission.PermissionId equals permission.Id
                where userRole.UserId == userId
                select permission.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlySet<string>> GetPermissionNamesByRoleIdsAsync(IEnumerable<string> roleIds, CancellationToken cancellationToken = default)
        {
            var ids = roleIds.Distinct(StringComparer.Ordinal).ToArray();
            var names = await (
                from rolePermission in _context.RolePermissions
                join permission in _context.Permissions on rolePermission.PermissionId equals permission.Id
                where ids.Contains(rolePermission.RoleId)
                select permission.Name)
                .Distinct()
                .ToListAsync(cancellationToken);

            return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public Task<Property?> GetPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            return _context.Properties.AsNoTracking().SingleOrDefaultAsync(property => property.ID == propertyId, cancellationToken);
        }

        public async Task<IdentityResult> CreateWithRolesAsync(
            User user,
            string password,
            IReadOnlyList<string> roleNames,
            AuditLog auditLog,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return createResult;
                }

                if (roleNames.Count > 0)
                {
                    var roleResult = await _userManager.AddToRolesAsync(user, roleNames);
                    if (!roleResult.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return roleResult;
                    }
                }
                auditLog.EntityId = user.Id;
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return IdentityResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IdentityResult> UpdateWithAuditAsync(
            User user,
            AuditLog auditLog,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return updateResult;
                }

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return IdentityResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IdentityResult> ReplaceRolesAsync(
            User user,
            IReadOnlyList<string> roleNames,
            AuditLog auditLog,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return removeResult;
                }

                if (roleNames.Count > 0)
                {
                    var addResult = await _userManager.AddToRolesAsync(user, roleNames);
                    if (!addResult.Succeeded)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return addResult;
                    }
                }

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return IdentityResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public Task AddAuditAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _context.AuditLogs.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<User> BuildUserQuery(
      string? userName,
      string? fullName,
      bool? isActive,
      int? propertyId,
      string? roleId,
      DateTime? createdFrom,
      DateTime? createdTo)
        {
            var query = _context.Users
                .Include(user => user.Property)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var value = userName.Trim().ToLower();

                query = query.Where(user =>
                    user.UserName != null &&
                    user.UserName.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var value = fullName.Trim().ToLower();

                query = query.Where(user =>
                    user.FullName.ToLower().Contains(value));
            }

            if (isActive.HasValue)
            {
                query = query.Where(user =>
                    user.IsActive == isActive.Value);
            }

            if (propertyId.HasValue)
            {
                query = query.Where(user =>
                    user.PropertyId == propertyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(roleId))
            {
                query = query.Where(user =>
                    _context.UserRoles.Any(userRole =>
                        userRole.UserId == user.Id &&
                        userRole.RoleId == roleId));
            }

            if (createdFrom.HasValue)
            {
                query = query.Where(user =>
                    user.CreatedAt >= createdFrom.Value);
            }

            if (createdTo.HasValue)
            {
                query = query.Where(user =>
                    user.CreatedAt <= createdTo.Value);
            }

            return query;
        }
    }
}