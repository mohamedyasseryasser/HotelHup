using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class ReservationRoom:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public int ReservationId { get; set; }

        public int RoomId { get; set; }

        public int Adults { get; set; }
        public decimal NightlyRate { get; set; }

        public int Children { get; set; }

        public decimal TotalAmount { get; set; }

        // Snapshot of applied rate

        public string? RateSnapshot { get; set; }

        // Navigation Properties

        public Reservation Reservation { get; set; } = null!;

        public Room Room { get; set; } = null!;
    }
}