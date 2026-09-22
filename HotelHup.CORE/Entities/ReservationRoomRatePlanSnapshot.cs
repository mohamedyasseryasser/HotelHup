using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{

    public class ReservationRoomRatePlanSnapshot : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required,ForeignKey(nameof(ReservationRoom))]
        public int ReservationRoomId { get; set; }

        [Required,ForeignKey(nameof(RatePlan))]
        public int RatePlanId { get; set; }

        [Required,ForeignKey(nameof(RatePlanVersion))]
        public int RatePlanVersionId { get; set; }

        [Required, MaxLength(150)]
        public string RatePlanName { get; set; } = string.Empty;

        public RatePlanType RatePlanType { get; set; }

        public bool IsRefundable { get; set; }

        [Required, MaxLength(4000)]
        public string RulesSnapshot { get; set; } = "{}";

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

        public DateTimeOffset CapturedAt { get; set; }

        /*
         * Navigation Properties.
         */

        public ReservationRoom? ReservationRoom { get; set; }
            = null!;

        public RatePlan RatePlan { get; set; }
            = null!;

        public RatePlanVersion RatePlanVersion { get; set; }
            = null!;
    }
}