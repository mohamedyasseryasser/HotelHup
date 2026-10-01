using System.ComponentModel.DataAnnotations;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.DTO.Expense;

public sealed class CreateExpenseDto
{
    [Range(1, int.MaxValue)] public int PropertyId { get; init; }
    [Required, StringLength(100)] public string Category { get; init; } = string.Empty;
    [Required, StringLength(1000)] public string Description { get; init; } = string.Empty;
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] public decimal Amount { get; init; }
    [Required, StringLength(3, MinimumLength = 3)] public string Currency { get; init; } = string.Empty;
    public DateTimeOffset ExpenseDate { get; init; }
    [EnumDataType(typeof(PaymentMethod))] public PaymentMethod PaymentMethod { get; init; }
    [StringLength(200)] public string? VendorName { get; init; }
    [StringLength(100)] public string? ReferenceNumber { get; init; }
    [StringLength(2000)] public string? Notes { get; init; }
}

public sealed class UpdateExpenseDto
{
    [Required, StringLength(100)] public string Category { get; init; } = string.Empty;
    [Required, StringLength(1000)] public string Description { get; init; } = string.Empty;
    [StringLength(200)] public string? VendorName { get; init; }
    [StringLength(100)] public string? ReferenceNumber { get; init; }
    [StringLength(2000)] public string? Notes { get; init; }
    [Required] public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class VoidExpenseDto
{
    [Required, StringLength(1000)] public string Reason { get; init; } = string.Empty;
    [Required] public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class ExpenseQueryDto : pagination
{
    public int? PropertyId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public string? Category { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public ExpenseStatus? Status { get; init; }
    public string? VendorName { get; init; }
    public string? SortBy { get; init; }
    public bool Descending { get; init; }
}

public sealed class ExpenseResponseDto
{
    public int Id { get; init; }
    public int PropertyId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset ExpenseDate { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string? VendorName { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public ExpenseStatus Status { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string RowVersion { get; init; } = string.Empty;
} 

public sealed class ExpenseReportRequest
{
    [Range(1, int.MaxValue)]
    public int? PropertyId { get; init; }

    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    [StringLength(100)]
    public string? Category { get; init; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod? PaymentMethod { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string? Currency { get; init; }

    public bool IncludeVoided { get; init; }
}

public sealed class ExpenseReportResponse
{
    public int PropertyId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public decimal TotalExpenses { get; init; }
    public int ExpenseCount { get; init; }
    public IReadOnlyList<ExpenseReportCurrencySummary> ByCurrency { get; init; } = Array.Empty<ExpenseReportCurrencySummary>();
    public IReadOnlyList<ExpenseReportCategorySummary> ByCategory { get; init; } = Array.Empty<ExpenseReportCategorySummary>();
    public IReadOnlyList<ExpenseReportItem> Items { get; init; } = Array.Empty<ExpenseReportItem>();
}

public sealed class ExpenseReportCurrencySummary
{
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int Count { get; init; }
}

public sealed class ExpenseReportCategorySummary
{
    public string Category { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int Count { get; init; }
}

public sealed class ExpenseReportItem
{
    public int Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset ExpenseDate { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string? VendorName { get; init; }
    public string? ReferenceNumber { get; init; }
    public ExpenseStatus Status { get; init; }
}

