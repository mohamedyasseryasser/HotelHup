using HotelHup.APPLICATION.DTO.Reports;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.INFRASTRUCTURE.repos;

public sealed class ReportRepository : IReportRepository
{
    private readonly hotelhupContext _context;

    public ReportRepository(hotelhupContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? category,
        PaymentMethod? paymentMethod,
        string? currency,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.ExpenseDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.ExpenseDate <= to.Value);
        }

        if (!includeVoided)
        {
            query = query.Where(
                x => x.Status == ExpenseStatus.Posted);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(
                x => x.Category == category);
        }

        if (paymentMethod.HasValue)
        {
            query = query.Where(
                x => x.PaymentMethod == paymentMethod.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(
                x => x.Currency == currency);
        }

        return await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(
        int propertyId,
        int? roomTypeId,
        RoomStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(x => x.RoomType)
            .Where(x => x.property_id == propertyId);

        if (roomTypeId.HasValue)
        {
            query = query.Where(
                x => x.RoomTypeId == roomTypeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Status == status.Value);
        }

        return await query
            .OrderBy(x => x.RoomNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetReservationsAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        ReservationStatus? status,
        int? roomTypeId,
        BookingSource? source,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Reservations
            .AsNoTracking()
            .Include(x => x.Guest)
            .Include(x => x.Folio)
                .ThenInclude(x => x!.Payments)
            .Include(x => x.ReservationRooms)
                .ThenInclude(x => x.Room)
                    .ThenInclude(x => x!.RoomType)
            .Where(x => x.PropertyId == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.CheckOutDate > from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.CheckInDate < to.Value.Date.AddDays(1));
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Status == status.Value);
        }

        if (roomTypeId.HasValue)
        {
            query = query.Where(
                x => x.ReservationRooms.Any(
                    r => r.Room != null &&
                         r.Room.RoomTypeId == roomTypeId.Value));
        }

        if (source.HasValue)
        {
            query = query.Where(
                x => x.Source == source.Value);
        }

        return await query
            .OrderBy(x => x.CheckInDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolioItem>> GetRevenueItemsAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        FolioItemType? type,
        int? roomTypeId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FolioItems
            .AsNoTracking()
            .Include(x => x.Folio)
                .ThenInclude(x => x.Reservation)
                    .ThenInclude(x => x!.ReservationRooms)
            .Where(x =>
                x.Folio.Reservation.PropertyId == propertyId &&
                x.Status == FolioItemStatus.Posted);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.PostedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.PostedAt <= to.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(
                x => x.Type == type.Value);
        }

        if (roomTypeId.HasValue)
        {
            query = query.Where(
                x => x.Folio
                    .Reservation
                    .ReservationRooms
                    .Any(
                        r => r.Room != null &&
                             r.Room.RoomTypeId == roomTypeId.Value));
        }

        return await query
            .OrderBy(x => x.PostedAt)
            .ThenBy(x => x.id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        PaymentStatus? status,
        PaymentMethod? method,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(x => x.Folio)
                .ThenInclude(x => x.Reservation)
            .Where(x =>
                x.Folio.Reservation.PropertyId == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.PaidAt.HasValue &&
                     x.PaidAt.Value >= from.Value.DateTime);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.PaidAt.HasValue &&
                     x.PaidAt.Value <= to.Value.DateTime);
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Status == status.Value);
        }

        if (method.HasValue)
        {
            query = query.Where(
                x => x.Method == method.Value);
        }

        return await query
            .OrderByDescending(x => x.PaidAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Refund>> GetRefundsAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        RefundStatus? status,
        PaymentMethod? method,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Refunds
            .AsNoTracking()
            .Include(x => x.Folio)
                .ThenInclude(x => x.Reservation)
            .Where(x =>
                x.Folio.Reservation.PropertyId == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.CreatedAt <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Status == status.Value);
        }

        if (method.HasValue)
        {
            query = query.Where(
                x => x.Method == method.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationStatusHistory>> GetStatusHistoryAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        ReservationStatus? status,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReservationStatusHistories
            .AsNoTracking()
            .Include(x => x.Reservation)
                .ThenInclude(x => x.Guest)
            .Where(x =>
                x.Reservation.PropertyId == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => x.ChangedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => x.ChangedAt <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.ToStatus == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            query = query.Where(
                x => x.Reason != null &&
                     x.Reason.Contains(reason));
        }

        return await query
            .OrderByDescending(x => x.ChangedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HousekeepingTask>> GetHousekeepingTasksAsync(
        int propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        HousekeepingTaskStatus? status,
        int? roomTypeId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.HousekeepingTasks
            .AsNoTracking()
            .Include(x => x.Room)
                .ThenInclude(x => x!.RoomType)
            .Where(x =>
                x.Room.property_id == propertyId);

        if (from.HasValue)
        {
            query = query.Where(
                x => (x.DueAt ?? x.CreatedAt) >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(
                x => (x.DueAt ?? x.CreatedAt) <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(
                x => x.Status == status.Value);
        }

        if (roomTypeId.HasValue)
        {
            query = query.Where(
                x => x.Room.RoomTypeId == roomTypeId.Value);
        }

        return await query
            .OrderBy(x => x.DueAt)
            .ThenBy(x => x.id)
            .ToListAsync(cancellationToken);
    }

 
}