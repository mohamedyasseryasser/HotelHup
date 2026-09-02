using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{

    public interface IUserRepository
    {
          Task<User?> GetuserByIdAsync( string userId, CancellationToken cancellationToken = default);

         Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<User?> GetByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<User>> GetListAsync(
            string? userName,
            string? fullName,
            bool? isActive,
            int? propertyId,
            string? roleId,
            DateTime? createdFrom,
            DateTime? createdTo,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<int> CountAsync(
            string? userName,
            string? fullName,
            bool? isActive,
            int? propertyId,
            string? roleId,
            DateTime? createdFrom,
            DateTime? createdTo,
            CancellationToken cancellationToken = default);
        Task<bool> ExistsByNormalizedUserNameAsync(string normalizedUserName, string? excludingUserId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Role>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlySet<string>> GetPermissionNamesByRoleIdsAsync(IEnumerable<string> roleIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Role>> GetRolesByIdsAsync(IEnumerable<string> roleIds, CancellationToken cancellationToken = default);
        Task<IReadOnlySet<string>> GetPermissionNamesAsync(string userId, CancellationToken cancellationToken = default);
        Task<Property?> GetPropertyAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<IdentityResult> CreateWithRolesAsync(User user, string password, IReadOnlyList<string> roleNames, AuditLog auditLog, CancellationToken cancellationToken = default);
        Task<IdentityResult> UpdateWithAuditAsync(User user, AuditLog auditLog, CancellationToken cancellationToken = default);
        Task<IdentityResult> ReplaceRolesAsync(User user, IReadOnlyList<string> roleNames, AuditLog auditLog, CancellationToken cancellationToken = default);
        Task AddAuditAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}