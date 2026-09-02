using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{


    public interface IPropertyRepository
    {
        Task<Property?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Property>> GetListAsync(string? search, PropertyStatus? status, string sortBy, bool descending, int skip, int take, CancellationToken cancellationToken = default);
        Task<int> CountAsync(string? search, PropertyStatus? status, CancellationToken cancellationToken = default);
        Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default);
        Task<bool> HasActiveReservationsAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<bool> HasOpenFoliosAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<bool> HasFinancialTransactionsAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<PropertySettings?> GetSettingsAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Tax>> GetTaxesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<Tax?> GetTaxAsync(int propertyId, int taxId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CancellationPolicy>> GetCancellationPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<CancellationPolicy?> GetCancellationPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DepositPolicy>> GetDepositPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<DepositPolicy?> GetDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
        Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
        Task AddAuditLogAsync(AddAuditLogDto auditLog, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

}
