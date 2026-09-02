using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.CORE.Entities;

public class DepositPolicy : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public int PropertyId { get; set; }
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    public DepositType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public Property Property { get; set; } = null!;
}
