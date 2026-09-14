using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IRoomTypeRepository
    {
        Task<List<RoomType>> GetAllAsync(int? propertyId, bool includeInactive, CancellationToken ct = default);
        Task<RoomType?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default);
        Task<Property?> GetPropertyAsync(int id, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, int propertyId, int? excludingId = null, CancellationToken ct = default);
        Task SaveAsync(RoomType roomType, AddAuditLogDto audit, CancellationToken ct = default);
    }
}
