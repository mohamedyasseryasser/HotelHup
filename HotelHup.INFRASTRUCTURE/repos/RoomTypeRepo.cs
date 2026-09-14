using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class RoomTypeRepository : IRoomTypeRepository
    {
        private readonly hotelhupContext _context;

        public RoomTypeRepository(hotelhupContext context) => _context = context;

        public async Task<List<RoomType>> GetAllAsync(int? propertyId, bool includeInactive, CancellationToken ct = default)
        {
            var query = _context.RoomTypes.AsNoTracking().AsQueryable();
            if (propertyId.HasValue)
                query = query.Where(x => x.property_id == propertyId.Value);
            if (!includeInactive)
                query = query.Where(x => x.IsActive);
            return await query.OrderBy(x => x.Name).ToListAsync(ct);
        }

        public Task<RoomType?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default)
        {
            IQueryable<RoomType> query = _context.RoomTypes;
            if (!tracking)
                query = query.AsNoTracking();
            return query.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<Property?> GetPropertyAsync(int id, CancellationToken ct = default) =>
         await   _context.Properties.SingleOrDefaultAsync(x => x.ID == id, ct);

        public async Task<bool> NameExistsAsync(string name, int propertyId, int? excludingId = null, CancellationToken ct = default)
        {
            var normalized = name.Trim();
            return await  _context.RoomTypes.AnyAsync(x =>
                x.property_id == propertyId &&
                x.Name == normalized &&
                (!excludingId.HasValue || x.Id != excludingId.Value), ct);
        }

        public async Task SaveAsync(RoomType roomType, AddAuditLogDto audit, CancellationToken ct = default)
        {
            if (_context.Entry(roomType).State == EntityState.Detached)
             await   _context.RoomTypes.AddAsync(roomType);
            else if (_context.Entry(roomType).State == EntityState.Modified)
                _context.Entry(roomType).Property(x => x.RowVersion).OriginalValue = roomType.RowVersion;
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = audit.userid,
                    PropertyId = roomType.property_id,
                    EntityName = audit.TargetEntity,
                    EntityId = audit.TargetEntityId,
                    Action = audit.Action,
                    OldValues = audit.OldValues is null ? null : JsonSerializer.Serialize(audit.OldValues),
                    NewValues = audit.NewValues is null ? null : JsonSerializer.Serialize(audit.NewValues),
                    Reason = audit.Reason,
                    CorrelationId = audit.CorrelationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = audit.userid
                });
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
