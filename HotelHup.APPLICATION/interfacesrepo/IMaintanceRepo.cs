using HotelHup.APPLICATION.DTO.maintenance;
using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.interfacesrepo;

public interface IMaintenanceRepository
{
    Task<(IReadOnlyList<MaintenanceTicket> Items, int TotalCount)> GetPagedAsync(
        MaintenanceListRequest request,
        CancellationToken ct = default);

    Task<MaintenanceTicket?> GetByIdAndPropertyAsync(
        int maintenanceId,
        int propertyId,
        bool tracking,
        CancellationToken ct = default);

    Task<Property?> GetPropertyAsync(int propertyId, CancellationToken ct = default);

    Task<Room?> GetRoomAsync(
        int propertyId,
        int roomId,
        bool tracking,
        CancellationToken ct = default);

    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);

    Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default);

    Task AddAuditAsync(AuditLog audit, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteSerializableAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default);
}
