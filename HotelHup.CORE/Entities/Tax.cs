using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.CORE.Entities;

public class Tax : BaseEntity
{
    [Key]
    public int Id { get; set; }
    public int PropertyId { get; set; }
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [Range(0, double.MaxValue)]
    public decimal Rate { get; set; }
    public TaxType Type { get; set; }
    public bool IsInclusive { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
       = Array.Empty<byte>();
    public Property Property { get; set; } = null!;
    public ICollection<ReservationTaxSnapshot> ReservationTaxSnapshots { get; set; }= new HashSet<ReservationTaxSnapshot>();
}
