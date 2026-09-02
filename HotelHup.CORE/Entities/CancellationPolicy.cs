using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.CORE.Entities;

public class CancellationPolicy : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public int PropertyId { get; set; }
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public PolicyStatus Status { get; set; } = PolicyStatus.Active;
    [MaxLength(4000)]
    public string Rules { get; set; } = "{}";
    public int Version { get; set; } = 1;
    public int FreeCancellationHours { get; set; }
    public decimal CancellationFeePercentage { get; set; }
    public decimal FixedCancellationFee { get; set; }
    public bool IsNonRefundable { get; set; }
    public int CutoffHours { get; set; }
    public Property Property { get; set; } = null!;
}
