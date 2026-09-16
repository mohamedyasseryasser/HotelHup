using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.guest;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IGuestRepository
    {
        Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Guest?> GetTrackedByIdAsync(int id, int propertyid,CancellationToken ct = default);
        Task<(IReadOnlyList<GuestListItemResponse> Items, int TotalCount)> GetListAsync(GuestListRequest request, int propertyid,CancellationToken ct = default);
        Task<IReadOnlyList<Guest>> FindPossibleDuplicatesAsync(int propertyid,string? phone, string? email, string? nationalId, string? identityNumber, int? excludingId = null, CancellationToken ct = default);
        Task<IReadOnlyList<GuestReservationSummaryResponse>> GetReservationHistoryAsync(int guestId, int propertyid,CancellationToken ct = default);
        Task<bool> HasReservationsAsync(int guestId, CancellationToken ct = default);
        Task<bool> HasFoliosAsync(int guestId, CancellationToken ct = default);
        Task AddAsync(Guest guest,
      AddAuditLogDto auditDto,
      CancellationToken ct = default);
        Task AddAuditLogAsync(AddAuditLogDto audit, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
     Task   PersistAsync(Guest guest, AddAuditLogDto audit, CancellationToken ct = default);
    }
}
