using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.folio
{

    public sealed class FolioResponseDto
    {
        public int Id { get; init; }
        public int ReservationId { get; init; }
        public FolioStatus Status { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public decimal Balance { get; init; }
        public decimal CreditBalance { get; init; }
        public IReadOnlyList<FolioItemResponseDto> Items { get; init; } = Array.Empty<FolioItemResponseDto>();
        public IReadOnlyList<PaymentResponseDto> Payments { get; init; } = Array.Empty<PaymentResponseDto>();
        public IReadOnlyList<RefundResponseDto> Refunds { get; init; } = Array.Empty<RefundResponseDto>();
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public string RowVersion { get; init; } = string.Empty;
    }

    public sealed class FolioItemResponseDto
    {
        public string? ServiceName { get; init; }

        public int Id { get; init; }
        public int FolioId { get; init; }
        public FolioItemType Type { get; init; }
        public decimal Amount { get; init; }
        public decimal Tax { get; init; }
        public FolioItemSource Source { get; init; }
        public FolioItemStatus Status { get; init; }
        public string? Description { get; init; }
        public string? ReferenceId { get; init; }
        public DateTimeOffset PostedAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string? CreatedBy { get; init; }
        public int? ServiceId { get; init; }

    }

    public sealed class PaymentResponseDto
    {
        public int Id { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod Method { get; init; }
        public PaymentStatus Status { get; init; }
        public string? ExternalId { get; init; }
        public DateTime? PaidAt { get; init; }

        public IReadOnlyList<RefundResponseDto> Refunds { get; init; } = Array.Empty<RefundResponseDto>();
    }

    public sealed class RefundResponseDto
    {
        public int Id { get; init; }
        public int PaymentId { get; init; }
        public decimal Amount { get; init; }
        public RefundStatus Status { get; init; }
        public string? Reason { get; init; }
        public string? ExternalId { get; init; }
    }

    public sealed class AddFolioItemRequestDto
    {
        [Required]
        public FolioItemType Type { get; init; }

        [Range(0.01, 999999999999.99)]
        public decimal Amount { get; init; }

        [Range(0, 999999999999.99)]
        public decimal Tax { get; init; }

        [Required]
        public FolioItemSource Source { get; init; }

        public int? ServiceId { get; init; }

        [MaxLength(500)]
        public string? Description { get; init; }

        [MaxLength(200)]
        public string? ReferenceId { get; init; }

        [MaxLength(100)]
        public string? IdempotencyKey { get; init; }
    }


    public sealed class VoidFolioItemRequestDto
    {
        [Required, MinLength(3), MaxLength(500)] public string Reason { get; init; } = string.Empty;
        public string? ExpectedRowVersion { get; init; }
    }

    public sealed class ReopenFolioRequestDto
    {
        [Required, MinLength(3), MaxLength(500)] public string Reason { get; init; } = string.Empty;
        public string? ExpectedRowVersion { get; init; }
    }

    public sealed class CloseFolioRequestDto
    {
        [Required, MinLength(3), MaxLength(500)] public string Reason { get; init; } = string.Empty;
        public string? ExpectedRowVersion { get; init; }
    }

    public sealed class FolioSummaryDto
    {
        public decimal GrossTotal { get; init; }
        public decimal Discounts { get; init; }
        public decimal Taxes { get; init; }
        public decimal Fees { get; init; }
        public decimal Refunds { get; init; }
        public decimal Payments { get; init; }
        public decimal Balance { get; init; }
        public decimal TotalCharges { get; init; }
    }

    public sealed class FolioItemPageRequest
    {
        [Range(1, 100)] public int PageSize { get; init; } = 20;
        [Range(1, int.MaxValue)] public int PageNumber { get; init; } = 1;
    }

    public sealed class FolioItemPageDto
    {
        public IReadOnlyList<FolioItemResponseDto> Items { get; init; } = Array.Empty<FolioItemResponseDto>();
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

}
