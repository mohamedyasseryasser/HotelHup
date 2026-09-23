using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.APPLICATION.DTO.reservation;

public sealed class AvailabilityRequest
{
    [Range(1, int.MaxValue)] public int PropertyId { get; init; }
    [Required] public DateTime CheckInDate { get; init; }
    [Required] public DateTime CheckOutDate { get; init; }
    [Range(1, 20)] public int Adults { get; init; } = 1;
    [Range(0, 20)] public int Children { get; init; }
    public int? RoomTypeId { get; init; }
    public int? ExcludedReservationId { get; init; }
}

public sealed class AvailabilityItemResponse
{
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public int RoomTypeId { get; init; }
    public string RoomTypeName { get; init; } = string.Empty;
    public decimal NightlyRate { get; init; }
    public int Nights { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed class CreateReservationRequest
{
    [Range(1, int.MaxValue)] public int PropertyId { get; init; }
    [Range(1, int.MaxValue)] public int GuestId { get; init; }
    [Required] public DateTime CheckInDate { get; init; }
    [Required] public DateTime CheckOutDate { get; init; }

    public BookingSource Source { get; init; } = BookingSource.Direct;
    // Preferred contract. The legacy property below remains supported for existing clients.
    //   public ICollection<CreateReservationRoomRequest> Rooms { get; init; } = new List<CreateReservationRoomRequest>();
    public ICollection<CreateReservationRoomRequest> createReservationRoomRequests { get; init; }
        = new List<CreateReservationRoomRequest>();
    public int? CancellationPolicyId { get; init; }
    public int? CancellationPolicyVersionId { get; init; }
    public int? DepositPolicyId { get; init; }
    public int? DepositPolicyVersionId { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
}
public sealed class CreateReservationRoomRequest
{
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal FeeAmount { get; set; }
    // Optional when the customer books by room type only.
    public int? RoomId { get; init; }
    // Required when RoomId is null.
    public int? RoomTypeId { get; init; }
    [Range(1, int.MaxValue)]
    public int RatePlanId { get; init; }
    [Range(1, 20)] public int Adults { get; init; } = 1;
    [Range(0, 20)] public int Children { get; init; }
}

public sealed class UpdateReservationRequest
{
    public DateTime? CheckInDate { get; init; }
    public DateTime? CheckOutDate { get; init; }
    [Range(1, 20)] public int? Adults { get; init; }
    [Range(0, 20)] public int? Children { get; init; }
    public int? GuestId { get; init; }
    public int? RoomId { get; init; }
    public int? RoomTypeId { get; init; }
    public int? RatePlanId { get; init; }
    public ICollection<UpdateReservationRoomRequest> Rooms { get; init; } 
        = new List<UpdateReservationRoomRequest>();
    public byte[]? ExpectedRowVersion { get; init; }
}

public sealed class UpdateReservationRoomRequest
{
    public int? ReservationRoomId { get; init; }
    public int? RoomId { get; init; }
    public int? RoomTypeId { get; init; }
    [Range(1, int.MaxValue)] public int? RatePlanId { get; init; }
    [Range(1, 20)] public int Adults { get; init; } = 1;
    [Range(0, 20)] public int Children { get; init; }
    [Range(0, double.MaxValue)] public decimal DiscountAmount { get; init; }
    [Range(0, double.MaxValue)] public decimal FeeAmount { get; init; }
}

public sealed class CancelReservationRequest
{
    [Required, MinLength(2), MaxLength(500)] public string Reason { get; init; } = string.Empty;
    public byte[]? ExpectedRowVersion { get; init; }
}

public sealed class ConfirmReservationRequest
{
    [MaxLength(500)] public string? Reason { get; init; }
    public byte[]? ExpectedRowVersion { get; init; }
}

public sealed class AssignRoomRequest
{
    // RoomId is kept for backward compatibility; Rooms is used for multi-room assignment.
    [Range(1, int.MaxValue)] public int RoomId { get; init; }
    public ICollection<AssignRoomItemRequest> Rooms { get; init; } = new List<AssignRoomItemRequest>();
    public int? RatePlanId { get; init; }
    [MaxLength(500)] public string? Reason { get; init; }
}

public sealed class AssignRoomItemRequest
{
    [Range(1, int.MaxValue)] public int RoomId { get; init; }
    public int? RatePlanId { get; init; }
    [Range(1, 20)] public int? Adults { get; init; }
    [Range(0, 20)] public int? Children { get; init; }
}

public sealed class CheckInRequest
{
    [Range(1, int.MaxValue)] public int? RoomId { get; init; }
    public ICollection<int> RoomIds { get; init; } = new List<int>();
    public bool IdentityVerified { get; init; }
    [MaxLength(100)] public string? IdentityType { get; init; }
    [MaxLength(100)] public string? IdentityReference { get; init; }
    [MaxLength(1000)] public string? RegistrationNotes { get; init; }
}

public sealed class CheckOutRequest
{
    [MaxLength(1000)] public string? Notes { get; init; }
}

public sealed class NoShowRequest
{
    public byte[]? ExpectedRowVersion { get; init; }

    [MaxLength(500)] public string? Reason { get; init; }
}

public sealed class ReservationSearchRequest
{
    public int? PropertyId { get; init; }
    public ReservationStatus? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? GuestId { get; init; }
    public int? RoomId { get; init; }
    public BookingSource? Source { get; init; }
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
}

public sealed class ReservationRoomResponse
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public int RatePlanId { get; init; }
    public int RatePlanVersionId { get; init; }
    public int Nights { get; init; }
    public decimal NightlyRate { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal FeeAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public bool IsCurrent { get; init; }
}

public class ReservationSummaryResponse
{
    public int Id { get; init; }
    public int PropertyId { get; init; }
    public int GuestId { get; init; }
    public string GuestName { get; init; } = string.Empty;
    public ReservationStatus Status { get; init; }
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public decimal TotalAmount { get; init; }
    public int? RoomId { get; init; }
    public IReadOnlyList<int> RoomIds { get; init; } = Array.Empty<int>();
}

public sealed class ReservationResponse : ReservationSummaryResponse
{
    public int Adults { get; init; }
    public int Children { get; init; }
    public BookingSource Source { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal FeeAmount { get; init; }
    public IReadOnlyList<ReservationRoomResponse> Rooms { get; init; } = Array.Empty<ReservationRoomResponse>();
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class PagedReservationResponse
{
    public IReadOnlyList<ReservationSummaryResponse> Items { get; init; } = Array.Empty<ReservationSummaryResponse>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class ReservationActionResponse
{
    public int Id { get; init; }
    public ReservationStatus Status { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<int> RoomIds { get; init; } = Array.Empty<int>();
}

public sealed class ReservationStatusHistoryResponse
{
    public int Id { get; init; }
    public ReservationStatus? FromStatus { get; init; }
    public ReservationStatus ToStatus { get; init; }
    public string? Reason { get; init; }
    public string? ActorId { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
}
