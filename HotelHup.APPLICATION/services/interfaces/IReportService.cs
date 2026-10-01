using HotelHup.APPLICATION.DTO.Expense;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Reports;
using HotelHup.CORE.Entities;

namespace HotelHup.APPLICATION.services.interfaces;

public interface IReportService
{
    Task<ResponseStatus<OccupancyReportResponse>> GetOccupancyAsync(OccupancyReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<RevenueReportResponse>> GetRevenueAsync(RevenueReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<ArrivalReportItem>>> GetArrivalsAsync(ReservationReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<DepartureReportItem>>> GetDeparturesAsync(ReservationReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<CurrentGuestReportItem>>> GetCurrentGuestsAsync(ReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<RoomStatusReportItem>>> GetRoomStatusAsync(RoomStatusReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<PaymentReportItem>>> GetPaymentsAsync(PaymentReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<RefundReportItem>>> GetRefundsAsync(RefundReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<CancellationReportItem>>> GetCancellationsAsync(CancellationReportRequest request, User actor, CancellationToken ct = default);
    Task<ResponseStatus<IReadOnlyList<HousekeepingReportItem>>> GetHousekeepingAsync(HousekeepingReportRequest request, User actor, CancellationToken ct = default);
     Task<ResponseStatus<ExpenseReportResponse>> GetExpenseReportAsync(ExpenseReportRequest request, User actor, CancellationToken ct = default);
}
