using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.interfacesrepo;

public interface IReportRepository
{
    Task<IReadOnlyList<Expense>> GetExpensesAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, string? category, PaymentMethod? paymentMethod, string? currency, bool includeVoided, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetRoomsAsync(int propertyId, int? roomTypeId, RoomStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetReservationsAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, ReservationStatus? status, int? roomTypeId, BookingSource? source, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FolioItem>> GetRevenueItemsAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, FolioItemType? type, int? roomTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, PaymentStatus? status, PaymentMethod? method, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Refund>> GetRefundsAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, RefundStatus? status, PaymentMethod? method, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReservationStatusHistory>> GetStatusHistoryAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, ReservationStatus? status, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HousekeepingTask>> GetHousekeepingTasksAsync(int propertyId, DateTimeOffset? from, DateTimeOffset? to, HousekeepingTaskStatus? status, int? roomTypeId, CancellationToken cancellationToken = default);
 }
