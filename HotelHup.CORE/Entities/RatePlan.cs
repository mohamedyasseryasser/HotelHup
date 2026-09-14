using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{
    public class RatePlan:BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public RatePlanType Type { get; set; }

        public string Rules { get; set; } = string.Empty;

        public bool IsRefundable { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation Properties

        [ForeignKey(nameof(PropertyId))]
        public Property Property { get; set; } = null!;

        [ForeignKey(nameof(RoomTypeId))]
        public RoomType RoomType { get; set; } = null!;

        public ICollection<RatePlanVersion> Versions { get; set; }
            = new List<RatePlanVersion>();

        public ICollection<Reservation> Reservations { get; set; }
            = new List<Reservation>();
    }
}