using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.CORE.Entities;

public class Property : BaseEntity
{
    [Key]
    public int ID { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string? Description { get; set; }
     [Required]
    public PropertyStatus Status { get; set; } = PropertyStatus.Active;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public PropertySettings? Settings { get; set; }
    public ICollection<Tax> Taxes { get; set; } = new List<Tax>();
    public ICollection<CancellationPolicy> CancellationPolicies { get; set; } = new List<CancellationPolicy>();
    public ICollection<DepositPolicy> DepositPolicies { get; set; } = new List<DepositPolicy>();
}
