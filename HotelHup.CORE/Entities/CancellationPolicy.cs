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
    public int CurrentVersion { get; set; } = 1;
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public PolicyStatus Status { get; set; } = PolicyStatus.Active;

    public Property Property { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; }
        = new List<Reservation>();
    public ICollection<CancellationPolicyVersion> Versions { get; set; }
        = new List<CancellationPolicyVersion>();
}
