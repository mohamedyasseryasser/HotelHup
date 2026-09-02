using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property
{
    public sealed class DeactivatePropertyRequest
    {
        [Required, StringLength(500, MinimumLength = 2)] public string Reason { get; init; } = string.Empty;
    }

    public sealed class PropertySettingsResponse
    {
        public int PropertyId { get; init; }
        public TimeSpan CheckInTime { get; init; }
        public TimeSpan CheckOutTime { get; init; }
        public bool AllowEarlyCheckIn { get; init; }
        public TimeSpan? HotelDayClosingTime { get; init; }
        public string DefaultCurrency { get; init; } = string.Empty;
        public string TimeZone { get; init; } = string.Empty;
        public bool RequireDepositForReservation { get; init; }
        public bool RequireFullPaymentBeforeCheckOut { get; init; }
        public bool AllowOverpayment { get; init; }
        public bool RequireInspectionBeforeAvailable { get; init; }
    }

    public sealed class UpdatePropertySettingsRequest
    {
        public TimeSpan? CheckInTime { get; init; }
        public TimeSpan? CheckOutTime { get; init; }
        public bool? AllowEarlyCheckIn { get; init; }
        public TimeSpan? HotelDayClosingTime { get; init; }
        [StringLength(10)] public string? DefaultCurrency { get; init; }
        public string? TimeZone { get; init; }
        public bool? RequireDepositForReservation { get; init; }
        public bool? RequireFullPaymentBeforeCheckOut { get; init; }
        public bool? AllowOverpayment { get; init; }
        public bool? RequireInspectionBeforeAvailable { get; init; }
        public string? Reason { get; init; }
    }

    public sealed class CreateTaxRequest
    {
        [Required, StringLength(150)] public string Name { get; init; } = string.Empty;
        [Required, StringLength(50)] public string Code { get; init; } = string.Empty;
        [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal Rate { get; init; }
        public TaxType Type { get; init; }
        public bool IsInclusive { get; init; }
        public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ValidTo { get; init; }
    }

    public sealed class UpdateTaxRequest
    {
        [StringLength(150)] public string? Name { get; init; }
        [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal? Rate { get; init; }
        public TaxType? Type { get; init; }
        public bool? IsInclusive { get; init; }
        public DateTimeOffset? ValidFrom { get; init; }
        public DateTimeOffset? ValidTo { get; init; }
        public string? Reason { get; init; }
    }

    public sealed class TaxResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public decimal Rate { get; init; }
        public TaxType Type { get; init; }
        public bool IsInclusive { get; init; }
        public bool IsActive { get; init; }
        public DateTimeOffset ValidFrom { get; init; }
        public DateTimeOffset? ValidTo { get; init; }
    }

    public class CreateCancellationPolicyRequest
    {
        [Required, StringLength(150)] public string Name { get; init; } = string.Empty;
        [StringLength(1000)] public string? Description { get; init; }
        public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ValidTo { get; init; }
        public int FreeCancellationHours { get; init; }
        public decimal CancellationFeePercentage { get; init; }
        public decimal FixedCancellationFee { get; init; }
        public bool IsNonRefundable { get; init; }
        public int CutoffHours { get; init; }
        public string Rules { get; init; } = "{}";
    }

    public sealed class UpdateCancellationPolicyRequest : CreateCancellationPolicyRequest
    {
        public string? Reason { get; init; }
    }

    public sealed class CancellationPolicyResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTimeOffset ValidFrom { get; init; }
        public DateTimeOffset? ValidTo { get; init; }
        public PolicyStatus Status { get; init; }
        public string Rules { get; init; } = "{}";
        public int Version { get; init; }
        public int FreeCancellationHours { get; init; }
        public decimal CancellationFeePercentage { get; init; }
        public decimal FixedCancellationFee { get; init; }
        public bool IsNonRefundable { get; init; }
        public int CutoffHours { get; init; }
    }

    public class CreateDepositPolicyRequest
    {
        [Required, StringLength(150)] public string Name { get; init; } = string.Empty;
        public DepositType Type { get; init; }
        public decimal Amount { get; init; }
        public decimal Percentage { get; init; }
        public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ValidTo { get; init; }
    }

    public sealed class UpdateDepositPolicyRequest : CreateDepositPolicyRequest
    {
        public string? Reason { get; init; }
    }

    public sealed class DepositPolicyResponse
    {
        public int Id { get; init; }
        public int PropertyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public DepositType Type { get; init; }
        public decimal Amount { get; init; }
        public decimal Percentage { get; init; }
        public DateTimeOffset ValidFrom { get; init; }
        public DateTimeOffset? ValidTo { get; init; }
        public bool IsActive { get; init; }
        public int Version { get; init; }
    }
}
