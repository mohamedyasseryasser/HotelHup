using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelHup.INFRASTRUCTURE.repos;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly hotelhupContext _context;
    public ReservationRepository(hotelhupContext context) => _context = context;

    public Task<Property?> GetPropertyAsync(int propertyId, CancellationToken ct = default) =>
        _context.Properties.AsNoTracking().Include(x => x.Settings).SingleOrDefaultAsync(x => x.ID == propertyId, ct);

    public Task<Guest?> GetGuestAsync(int propertyId, int guestId, CancellationToken ct = default) =>
        _context.Guests.AsNoTracking().SingleOrDefaultAsync(x => x.propertyid == propertyId && x.Id == guestId, ct);

    public Task<Room?> GetRoomAsync(int propertyId, int roomId, bool tracking, CancellationToken ct = default)
    {
        IQueryable<Room> query = _context.Rooms
            .Include(x => x.RoomType)
                .ThenInclude(x => x!.RatePlans)
            .Where(x => x.property_id == propertyId && x.Id == roomId);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<RatePlan?> GetRatePlanAsync(
        int propertyId,
        int roomTypeId,
        int? ratePlanId,
        DateTime stayDate,
        CancellationToken ct = default)
    {
        var query = _context.RatePlans.AsNoTracking().Include(x => x.Versions)
            .Where(x => x.PropertyId == propertyId && x.RoomTypeId == roomTypeId && x.IsActive &&
                        x.ValidFrom.Date <= stayDate.Date && (x.ValidTo == null || x.ValidTo.Value.Date > stayDate.Date));
        if (ratePlanId.HasValue) query = query.Where(x => x.Id == ratePlanId.Value);
        return await query.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
    }

    public Task<CancellationPolicyVersion?> GetCancellationPolicyVersionAsync(int propertyId, int policyId, int? versionId, DateTimeOffset stayDate, CancellationToken ct = default)
    {
        var query = _context.cancellationPolicyVersions.AsNoTracking().Include(x => x.CancellationPolicy)
            .Where(x => x.CancellationPolicy != null && x.CancellationPolicy.PropertyId == propertyId &&
                        x.CancellationPolicyId == policyId && x.ValidFrom <= stayDate && (x.ValidTo == null || x.ValidTo > stayDate));
        if (versionId.HasValue) query = query.Where(x => x.Id == versionId.Value);
        return query.OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct);
    }

    public Task<DepositPolicyVersion?> GetDepositPolicyVersionAsync(int propertyId, int policyId, int? versionId, DateTimeOffset stayDate, CancellationToken ct = default)
    {
        var query = _context.depositPolicyVersions.AsNoTracking().Include(x => x.Deposit)
            .Where(x => x.Deposit != null && x.Deposit.PropertyId == propertyId && x.DepositId == policyId && x.IsActive &&
                        x.ValidFrom <= stayDate && (x.ValidTo == null || x.ValidTo > stayDate));
        if (versionId.HasValue) query = query.Where(x => x.Id == versionId.Value);
        return query.OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Tax>>
        GetActiveTaxesAsync(int propertyId,
        DateTimeOffset stayDate,
        CancellationToken ct = default) =>
        await _context.Taxes.AsNoTracking().Where(
            x => x.PropertyId == propertyId &&
            x.IsActive &&
            x.ValidFrom <= stayDate &&
            (x.ValidTo == null || x.ValidTo > stayDate)).
        OrderBy(x => x.Id).
        ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetAvailableRoomsAsync(int propertyId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children,
        int? roomTypeId,
        int? excludedReservationId,
        CancellationToken ct = default)
    {
        //check property is active and roomtype is active
        //get rooms belong propertyid and roomtype id
        //get rooms are active
        //check num of childern and adults
        //check double reservation
        var blockingStatuses = new[] { ReservationStatus.Pending, ReservationStatus.Confirmed, ReservationStatus.CheckedIn };
        var occupiedRoomIds = _context.ReservationRooms.Where(x => x.IsCurrent && x.Reservation != null &&
            x.Reservation.PropertyId == propertyId && blockingStatuses.Contains(x.Reservation.Status) &&
            (!excludedReservationId.HasValue || x.ReservationId != excludedReservationId.Value) &&
            x.CheckInDate < checkOut && x.CheckOutDate > checkIn).Select(x => x.RoomId);

        return await _context.Rooms.AsNoTracking().Include(x => x.RoomType)
            .Where(x => x.property_id == propertyId && x.IsActive &&
                        (x.Status == RoomStatus.Available || x.Status == RoomStatus.Inspected) &&
                        (!roomTypeId.HasValue || x.RoomTypeId == roomTypeId.Value) && x.RoomType != null &&
                        x.RoomType.IsActive && x.RoomType.MaxAdults >= adults && x.RoomType.MaxChildren >= children &&
                        !occupiedRoomIds.Contains(x.Id)).OrderBy(x => x.RoomNumber).ToListAsync(ct);
    }

    public async Task<Reservation?> GetByIdAsync(int propertyId, int id, bool tracking, CancellationToken ct = default)
    {
        IQueryable<Reservation> query = _context.Reservations
            .Include(x => x.Guest)
            .Include(x => x.ReservationRooms).ThenInclude(x => x.Room)
            .Include(x => x.ReservationRooms).ThenInclude(x => x.RatePlanSnapshot)
            .Include(x => x.StatusHistory)
            .Include(x => x.Folio).ThenInclude(x => x!.Items)
            .Include(x => x.Folio).ThenInclude(x => x!.Payments).ThenInclude(x => x.Refunds)
            .Include(x => x.CancellationPolicySnapshot)
            .Include(x => x.DepositPolicySnapshot)
            .Where(x => x.PropertyId == propertyId && x.Id == id);
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> SearchAsync(int propertyId, ReservationSearchRequest request, CancellationToken ct = default)
    {
        var query = _context.Reservations.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.From.HasValue) query = query.Where(x => x.CheckOutDate > request.From.Value.Date);
        if (request.To.HasValue) query = query.Where(x => x.CheckInDate < request.To.Value.Date.AddDays(1));
        if (request.GuestId.HasValue) query = query.Where(x => x.GuestId == request.GuestId.Value);
        if (request.Source.HasValue) query = query.Where(x => x.Source == request.Source.Value);
        if (request.RoomId.HasValue) query = query.Where(x => x.ReservationRooms.Any(r => r.RoomId == request.RoomId.Value && r.IsCurrent));
        var total = await query.CountAsync(ct);
        var items = await query.Include(x => x.Guest).Include(x => x.ReservationRooms)
            .OrderByDescending(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(
        Reservation reservation,
        Folio folio,
        CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync(ct);
            folio.ReservationId = reservation.Id;
            _context.Folios.Add(folio);
            await _context.SaveChangesAsync(ct);
            return;
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync(ct);
            folio.ReservationId = reservation.Id;
            _context.Folios.Add(folio);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public async Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await operation();
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task PersistLifecycleAsync(Reservation reservation,
        IReadOnlyCollection<Room> rooms, IReadOnlyCollection<HousekeepingTask>? housekeepingTasks = null, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _context.Reservations.Update(reservation);
            foreach (var room in rooms) _context.Rooms.Update(room);
            if (housekeepingTasks is not null && housekeepingTasks.Count > 0) _context.HousekeepingTasks.AddRange(housekeepingTasks);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    public async Task PersistTransitionAsync(
    Reservation reservation,
    AuditLog audit,
    CancellationToken ct = default)
    {
        await using var tx =
            await _context.Database.BeginTransactionAsync(ct);

        try
        {
            _context.Entry(reservation).State =
                EntityState.Modified;

            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

}
