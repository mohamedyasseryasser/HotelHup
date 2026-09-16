using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.guest;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{
    public sealed class GuestRepository : IGuestRepository
    {
        private readonly Context.hotelhupContext _context;

        public GuestRepository(Context.hotelhupContext context)
        {
            _context = context;
        }

        public Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _context.Guests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        }

        public Task<Guest?> GetTrackedByIdAsync(int id, int propertyid,CancellationToken ct = default)
        {
            return _context.Guests.SingleOrDefaultAsync(x => x.Id == id &&x.propertyid == propertyid, ct);
        }

        public async Task<(IReadOnlyList<GuestListItemResponse> Items, int TotalCount)> GetListAsync(
            GuestListRequest request,int propertyid,
            CancellationToken ct = default)
        {
            IQueryable<Guest> query = _context.Guests
         .AsNoTracking()
         .Where(x => x.propertyid == propertyid);

            if (request.IsActive.HasValue)
            {
                query = query.Where(
                    x => x.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string search = request.Search.Trim().ToLower();

                query = query.Where(x =>
                    (x.FirstName + " " + x.LastName)
                        .ToLower()
                        .Contains(search)
                    ||
                    (x.Phone != null &&
                     x.Phone.ToLower().Contains(search))
                    ||
                    (x.Email != null &&
                     x.Email.ToLower().Contains(search)));
            }

            int totalCount = await query.CountAsync(ct);

            bool descending = string.Equals(
                request.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            query = request.SortBy.ToLower() switch
            {
                "email" =>
                    descending
                        ? query.OrderByDescending(x => x.Email)
                        : query.OrderBy(x => x.Email),

                "phone" =>
                    descending
                        ? query.OrderByDescending(x => x.Phone)
                        : query.OrderBy(x => x.Phone),

                "createdat" =>
                    descending
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt),

                _ =>
                    descending
                        ? query
                            .OrderByDescending(x => x.LastName)
                            .ThenByDescending(x => x.FirstName)
                        : query
                            .OrderBy(x => x.LastName)
                            .ThenBy(x => x.FirstName)
            };

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new GuestListItemResponse
                {
                    Id = x.Id,
                    FullName = (x.FirstName + " " + x.LastName).Trim(),
                    Phone = x.Phone,
                    Email = x.Email,
                    IsActive = x.IsActive,
                    ReservationCount = x.Reservations.Count,
                    RowVersion = Convert.ToBase64String(x.RowVersion)
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<IReadOnlyList<Guest>> FindPossibleDuplicatesAsync(
            int propertyid,string? phone,
            string? email,
            string? nationalId,
            string? identityNumber,
            int? excludingId = null,
            CancellationToken ct = default)
        {
            phone = Normalize(phone);
            email = Normalize(email);
            nationalId = Normalize(nationalId);
            identityNumber = Normalize(identityNumber);

            return await _context.Guests.AsNoTracking()
                .Where(x =>(x.propertyid==propertyid) && (!excludingId.HasValue || x.Id != excludingId.Value) &&
                    ((phone != null && x.Phone != null && x.Phone.ToLower() == phone) ||
                     (email != null && x.Email != null && x.Email.ToLower() == email) ||
                     (nationalId != null && x.NationalId != null && x.NationalId.ToLower() == nationalId) ||
                     (identityNumber != null && x.IdentityNumber != null && x.IdentityNumber.ToLower() == identityNumber)))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<GuestReservationSummaryResponse>> GetReservationHistoryAsync(int guestId, int propertyid,CancellationToken ct = default)
        {
            return await _context.Reservations.AsNoTracking()
                .Where(x => x.GuestId == guestId &&x.PropertyId==propertyid)
                .OrderByDescending(x => x.CheckInDate)
                .Select(x => new GuestReservationSummaryResponse
                {
                    ReservationId = x.Id,
                    PropertyId = x.PropertyId,
                    Status = (int)x.Status,
                    CheckInDate = x.CheckInDate,
                    CheckOutDate = x.CheckOutDate,
                    TotalAmount = x.TotalAmount
                })
                .ToListAsync(ct);
        }

        public Task<bool> HasReservationsAsync(int guestId, CancellationToken ct = default) =>
            _context.Reservations.AnyAsync(x => x.GuestId == guestId, ct);

        public Task<bool> HasFoliosAsync(int guestId, CancellationToken ct = default) =>
            _context.Folios.AnyAsync(x => x.Reservation.GuestId == guestId, ct);

        public async Task AddAsync(
      Guest guest,
      AddAuditLogDto auditDto,
      CancellationToken ct = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(ct);

            try
            {
                await _context.Guests.AddAsync(guest, ct);

                await _context.SaveChangesAsync(ct);

                auditDto.TargetEntityId = guest.Id.ToString();
                auditDto.NewValues = guest;
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = auditDto.userid,
                    PropertyId = auditDto.PropertyId,
                    EntityName = auditDto.TargetEntity,
                    EntityId = auditDto.TargetEntityId,
                    Action = auditDto.Action,
                    OldValues = auditDto.OldValues == null
                        ? null
                        : JsonSerializer.Serialize(auditDto.OldValues),
                    NewValues = auditDto.NewValues == null
                        ? null
                        : JsonSerializer.Serialize(auditDto.NewValues),
                    Reason = auditDto.Reason,
                    CorrelationId = auditDto.CorrelationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = auditDto.userid
                });

                await _context.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        public async Task AddAuditLogAsync(AddAuditLogDto dto, CancellationToken ct = default)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = dto.userid,
                PropertyId = dto.PropertyId,
                EntityName = dto.TargetEntity,
                EntityId = dto.TargetEntityId,
                Action = dto.Action,
                OldValues = dto.OldValues == null ? null : JsonSerializer.Serialize(dto.OldValues),
                NewValues = dto.NewValues == null ? null : JsonSerializer.Serialize(dto.NewValues),
                Reason = dto.Reason,
                CorrelationId = dto.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = dto.userid
            });
            await Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        public async Task PersistAsync(Guest guest, AddAuditLogDto audit, CancellationToken ct = default)
        {
            await using var transaction =
         await _context.Database.BeginTransactionAsync(ct);

            try
            {
                if (_context.Entry(guest).State == EntityState.Detached)
                {
                    await _context.Guests.AddAsync(guest, ct);
                }
                else if (_context.Entry(guest).State == EntityState.Modified)
                {
                    _context.Entry(guest)
                        .Property(x => x.RowVersion)
                        .OriginalValue = guest.RowVersion;
                }

                // Save Guest first → ID + RowVersion generated
                await _context.SaveChangesAsync(ct);

                // Now guest.Id is available
                audit.TargetEntityId = guest.Id.ToString();

                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = audit.userid,
                    PropertyId = audit.PropertyId,
                    EntityName = audit.TargetEntity,
                    EntityId = audit.TargetEntityId,
                    Action = audit.Action,
                    OldValues = audit.OldValues is null
                        ? null
                        : JsonSerializer.Serialize(audit.OldValues),
                    NewValues = audit.NewValues is null
                        ? null
                        : JsonSerializer.Serialize(audit.NewValues),
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
