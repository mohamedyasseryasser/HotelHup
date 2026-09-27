
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.Payments
{

    public sealed class CreatePaymentRequest
    {
        [Range(0.01, 999999999999.99)]
        public decimal Amount { get; init; }
        [Required]
        public PaymentMethod Method { get; init; }
        [MaxLength(200)]
        public string? ExternalId { get; init; }
        [MaxLength(200)]
        public string? IdempotencyKey { get; init; }
    }

    public sealed class RefundPaymentRequest
    {
        [Range(0.01, 999999999999.99)]
        public decimal Amount { get; init; }
        [Required, MinLength(3), MaxLength(500)]
        public string Reason { get; init; } = string.Empty;
        [MaxLength(200)]
        public string? ExternalId { get; init; }
    }

    public sealed class PaymentResponseDto
    {
        public int Id { get; init; }
        public int FolioId { get; init; }
        public decimal Amount { get; init; }
        public PaymentMethod Method { get; init; }
        public PaymentStatus Status { get; init; }
        public string? ExternalId { get; init; }
        public string? IdempotencyKey { get; init; }
        public DateTime? PaidAt { get; init; }
        public decimal RefundedAmount { get; init; }
        public IReadOnlyList<RefundResponseDto> Refunds { get; init; } = Array.Empty<RefundResponseDto>();
    }

    public sealed class RefundResponseDto
    {
        public int Id { get; init; }
        public int PaymentId { get; init; }
        public int FolioId { get; init; }
        public decimal Amount { get; init; }
        public RefundStatus Status { get; init; }
        public string? Reason { get; init; }
        public string? ExternalId { get; init; }
    }

    public sealed class PaymentSummaryDto
    {
        public int FolioId { get; init; }
        public decimal Total { get; init; }
        public decimal SuccessfulPayments { get; init; }
        public decimal Refunds { get; init; }
        public decimal Balance { get; init; }
        public IReadOnlyList<PaymentResponseDto> Payments { get; init; } = Array.Empty<PaymentResponseDto>();
    }

}
