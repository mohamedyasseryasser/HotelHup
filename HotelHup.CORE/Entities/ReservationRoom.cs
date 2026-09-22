using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{
    public class ReservationRoom:BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required,ForeignKey(nameof(Reservation))]
        public int ReservationId { get; set; }

        [Required,ForeignKey(nameof(Room))]
        public int RoomId { get; set; }

        [Range(1, 20)]
        public int Adults { get; set; }

        [Range(0, 20)]
        public int Children { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public int RatePlanId { get; set; }

        public int RatePlanVersionId { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal NightlyRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FeeAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /*
         * Room assignment history.
         */

        public bool IsCurrent { get; set; } = true;

        public DateTimeOffset AssignedAt { get; set; }

        public DateTimeOffset? ReleasedAt { get; set; }

        public string? AssignmentReason { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
            = Array.Empty<byte>();

      
        public ReservationRoomRatePlanSnapshot
            RatePlanSnapshot
        { get; set; } = null!;

        public RatePlan RatePlan { get; set; } = null!;

        public RatePlanVersion RatePlanVersion { get; set; } = null!;

        public Reservation? Reservation { get; set; } = null!;

        public Room? Room { get; set; } = null!;

    }
}