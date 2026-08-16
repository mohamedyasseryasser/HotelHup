using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelHup.CORE.Enums;
namespace HotelHup.CORE.Entities
{

    public class Reservation
    {
        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(30)]
        public string ReservationCode { get; set; } = string.Empty;

        public int GuestId { get; set; }
        [ForeignKey(nameof(GuestId))]
        public Guest Guest { get; set; } = null!;

        public int RoomId { get; set; }
        [ForeignKey(nameof(RoomId))]
        public Room Room { get; set; } = null!;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime? ActualCheckInDate { get; set; }
        public DateTime? ActualCheckOutDate { get; set; }

        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public string CreatedById { get; set; }
        [ForeignKey(nameof(CreatedById))]
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(300)]
        public string? CancellationReason { get; set; }

        // Navigation
        public Invoice? Invoice { get; set; }
    }
}
