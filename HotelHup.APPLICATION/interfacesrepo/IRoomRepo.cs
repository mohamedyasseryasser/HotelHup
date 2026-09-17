using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{

    public interface IRoomRepository
    {
        Task<Room?> GetByIdAsync(int propertyId, int roomId, bool tracking, CancellationToken ct = default);
        Task<(IReadOnlyList<Room> Items, int TotalCount)> GetListAsync(int propertyId, string? search, bool includeInactive, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<Property?> GetPropertyAsync(int propertyId, CancellationToken ct = default);
        Task<RoomType?> GetRoomTypeAsync(int propertyId, int roomTypeId, bool activeOnly, CancellationToken ct = default);
        Task<bool> RoomNumberExistsAsync(int propertyId, string roomNumber, int? excludingRoomId = null, CancellationToken ct = default);
        Task<bool> HasActiveReservationAsync(int propertyId, int roomId, CancellationToken ct = default);
        Task PersistAsync(Room room, AuditLog audit, CancellationToken ct = default);
    }

}
