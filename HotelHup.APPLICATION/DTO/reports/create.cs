using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.DTO.Reports;

public class ReportRequest
{
    [Range(1, int.MaxValue)] public int? PropertyId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed class OccupancyReportRequest : ReportRequest
{
    public int? RoomTypeId { get; init; }
}

public sealed class RevenueReportRequest : ReportRequest
{
    public FolioItemType? RevenueType { get; init; }
    public int? RoomTypeId { get; init; }
    [StringLength(3, MinimumLength = 3)] public string? Currency { get; init; }
}

public sealed class ReservationReportRequest : ReportRequest
{
    public ReservationStatus? Status { get; init; }
    public int? RoomTypeId { get; init; }
    public BookingSource? Source { get; init; }
}

public sealed class PaymentReportRequest : ReportRequest
{
    public PaymentStatus? Status { get; init; }
    public PaymentMethod? Method { get; init; }
}

public sealed class RefundReportRequest : ReportRequest
{
    public RefundStatus? Status { get; init; }
    public PaymentMethod? Method { get; init; }
}

public sealed class CancellationReportRequest : ReportRequest
{
    public ReservationStatus? Status { get; init; }
    [StringLength(500)] public string? Reason { get; init; }
}

public sealed class RoomStatusReportRequest : ReportRequest
{
    public RoomStatus? Status { get; init; }
    public int? RoomTypeId { get; init; }
}

public sealed class HousekeepingReportRequest : ReportRequest
{
    public HousekeepingTaskStatus? Status { get; init; }
    public int? RoomTypeId { get; init; }
}

 

public sealed class OccupancyReportResponse
{
    public int PropertyId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int TotalRooms { get; init; }
    public int SellableRooms { get; init; }
    public int OccupiedRooms { get; init; }
    public int OutOfOrderRooms { get; init; }
    public int AvailableRooms { get; init; }
    public decimal OccupancyPercentage { get; init; }
    public int SoldRoomNights { get; init; }
    public int AvailableRoomNights { get; init; }
}

public sealed class RevenueReportResponse
{
    public int PropertyId { get; init; }
    public string? Currency { get; init; }
    public decimal RoomRevenue { get; init; }
    public decimal ServiceRevenue { get; init; }
    public decimal Taxes { get; init; }
    public decimal Discounts { get; init; }
    public decimal Refunds { get; init; }
    public decimal Fees { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal Expenses { get; init; }
    public decimal OperatingProfit { get; init; }
    public int SoldRoomNights { get; init; }
    public decimal ADR { get; init; }
    public decimal RevPAR { get; init; }
}

public sealed class ArrivalReportItem
{
    public int ReservationId { get; init; }
    public string Guest { get; init; } = string.Empty;
    public string? Room { get; init; }
    public string? RoomType { get; init; }
    public DateTime CheckInDate { get; init; }
    public int Adults { get; init; }
    public int Children { get; init; }
    public ReservationStatus Status { get; init; }
    public BookingSource Source { get; init; }
    public decimal? FolioBalance { get; init; }
}

public sealed class DepartureReportItem
{
    public int ReservationId { get; init; }
    public string Guest { get; init; } = string.Empty;
    public string? Room { get; init; }
    public DateTime CheckOutDate { get; init; }
    public ReservationStatus Status { get; init; }
    public decimal? FolioBalance { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public bool LateDeparture { get; init; }
}

public sealed class CurrentGuestReportItem
{
    public int ReservationId { get; init; }
    public string Guest { get; init; } = string.Empty;
    public string? Room { get; init; }
    public string? RoomType { get; init; }
    public DateTime CheckInDate { get; init; }
    public DateTime ExpectedCheckOut { get; init; }
    public int Nights { get; init; }
    public ReservationStatus Status { get; init; }
}

public sealed class RoomStatusReportItem
{
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public RoomStatus Status { get; init; }
    public int? CurrentReservationId { get; init; }
    public string? HousekeepingStatus { get; init; }
 }

public sealed class PaymentReportItem
{
    public int PaymentId { get; init; }
    public int ReservationId { get; init; }
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
}

public sealed class RefundReportItem
{
    public int RefundId { get; init; }
    public int ReservationId { get; init; }
    public PaymentMethod Method { get; init; }
    public RefundStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CancellationReportItem
{
    public int ReservationId { get; init; }
    public string Guest { get; init; } = string.Empty;
    public ReservationStatus Status { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public decimal CancellationFee { get; init; }
}

public sealed class HousekeepingReportItem
{
    public int TaskId { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public HousekeepingTaskStatus Status { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? AssigneeId { get; init; }
}

 
