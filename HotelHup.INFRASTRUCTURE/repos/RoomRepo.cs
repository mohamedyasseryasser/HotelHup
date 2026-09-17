using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class RoomRepository : IRoomRepository
    {
        private readonly hotelhupContext _context;
        public RoomRepository(hotelhupContext context) => _context = context;

        public Task<Room?> GetByIdAsync(int propertyId, int roomId, bool tracking, CancellationToken ct = default)
        {
            IQueryable<Room> query = _context.Rooms;
            if (!tracking) query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(x => x.property_id == propertyId && x.Id == roomId, ct);
        }

        public async Task<(IReadOnlyList<Room> Items, int TotalCount)> GetListAsync(int propertyId, string? search, bool includeInactive, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Rooms.AsNoTracking().Where(x => x.property_id == propertyId);
            if (!includeInactive) query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.RoomNumber.Contains(search.Trim()));
            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.RoomNumber).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public async Task<Property?> GetPropertyAsync(int propertyId, CancellationToken ct = default) =>
         await   _context.Properties.AsNoTracking().FirstOrDefaultAsync(x => x.ID == propertyId, ct);

        public Task<RoomType?> GetRoomTypeAsync(int propertyId, int roomTypeId, bool activeOnly, CancellationToken ct = default)
        {
            var query = _context.RoomTypes.AsNoTracking().Where(x => x.property_id == propertyId && x.Id == roomTypeId);
            if (activeOnly) query = query.Where(x => x.IsActive);
            return query.FirstOrDefaultAsync(ct);
        }

        public async Task<bool> RoomNumberExistsAsync(int propertyId, string roomNumber, int? excludingRoomId = null, CancellationToken ct = default) =>
       await     _context.Rooms.AnyAsync(x => x.property_id == propertyId && x.RoomNumber == roomNumber.Trim() && (!excludingRoomId.HasValue || x.Id != excludingRoomId.Value), ct);

        public async Task<bool> HasActiveReservationAsync(int propertyId, int roomId, CancellationToken ct = default) =>
         await   _context.ReservationRooms.AnyAsync(x => x.RoomId == roomId && x.Reservation.PropertyId == propertyId &&
                (x.Reservation.Status == CORE.Enums.ReservationStatus.Pending ||
         x.Reservation.Status == CORE.Enums.ReservationStatus.Confirmed || 
         x.Reservation.Status == CORE.Enums.ReservationStatus.CheckedIn), ct);

        public async Task PersistAsync(Room room, AuditLog audit, CancellationToken ct = default)
        {
            
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                if (_context.Entry(room).State == EntityState.Detached) 
                { await _context.Rooms.AddAsync(room, ct); }
                await _context.SaveChangesAsync();
                await _context.AuditLogs.AddAsync(audit);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }

}
