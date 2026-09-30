using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.interfacesrepo;

public interface IServiceRepository
{
    Task<Property?> GetPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    Task<Service?> GetByIdAsync(
        int propertyId,
        int serviceId,
        bool tracking,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Service> Items, int TotalCount)> GetListAsync(
        int propertyId,
        string? search,
        bool includeInactive,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        int propertyId,
        string normalizedName,
        int? excludingServiceId = null,
        CancellationToken cancellationToken = default);

    Task PersistAsync(
        Service service,
        AuditLog audit,
        CancellationToken cancellationToken = default);
}
