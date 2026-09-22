using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class ReservationStatusHistory : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReservationId { get; set; }

        public ReservationStatus? FromStatus { get; set; }

        public ReservationStatus ToStatus { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public string? ActorId { get; set; }

        public DateTimeOffset ChangedAt { get; set; }

        public Reservation Reservation { get; set; }
            = null!;
    }
}