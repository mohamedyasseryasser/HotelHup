using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.interfacesrepo
{
public interface IPropertyRepository
{
        Task<Property> CreateAsync(
    Property property,
    PropertySettings settings,
    AddAuditLogDto auditLog,
    CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(
    Property property,
    AddAuditLogDto auditLog,
    CancellationToken cancellationToken = default);

        Task<Property?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Property?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Property>> GetListAsync(string? search, PropertyStatus? status, string sortBy, bool descending, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, PropertyStatus? status, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasActiveReservationsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenFoliosAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<bool> HasFinancialTransactionsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertySettings?> GetSettingsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertySettings?> GetTrackedSettingsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tax>> GetTaxesAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<Tax?> GetTaxAsync(int propertyId, int taxId, CancellationToken cancellationToken = default);
    Task<Tax?> GetTrackedTaxAsync(int propertyId, int taxId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CancellationPolicy>> GetCancellationPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<CancellationPolicy?> GetCancellationPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
    Task<CancellationPolicy?> GetTrackedCancellationPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
    Task<int> GetNextCancellationVersionAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepositPolicy>> GetDepositPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<DepositPolicy?> GetDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
    Task<DepositPolicy?> GetTrackedDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
    Task<int> GetNextDepositVersionAsync(int propertyId, CancellationToken cancellationToken = default);
    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task AddAuditLogAsync(AddAuditLogDto auditLog, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
}