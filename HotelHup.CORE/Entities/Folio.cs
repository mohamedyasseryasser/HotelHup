using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class Folio:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public int ReservationId { get; set; }

        public FolioStatus Status { get; set; }

        public string Currency { get; set; } = null!;

        public decimal Total { get; set; }

        public decimal Balance { get; set; }

        // Closing

        public DateTimeOffset? ClosedAt { get; set; }

        public string? ClosedBy { get; set; }

        // Navigation Properties

        public Reservation Reservation { get; set; } = null!;

        public ICollection<FolioItem> Items { get; set; }
            = new List<FolioItem>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}