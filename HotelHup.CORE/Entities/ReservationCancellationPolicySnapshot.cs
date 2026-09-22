using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{

    public class ReservationCancellationPolicySnapshot : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Reservation))]
        public int ReservationId { get; set; }
        [ForeignKey(nameof(CancellationPolicy))]
        public int? CancellationPolicyId { get; set; }

        [ForeignKey(nameof(CancellationPolicyVersion))]
        public int? CancellationPolicyVersionId { get; set; }

        [Required, MaxLength(150)]
        public string PolicyName { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Rules { get; set; } = "{}";

        public int FreeCancellationHours { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CancellationFeePercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedCancellationFee { get; set; }

        public bool IsNonRefundable { get; set; }

        public int CutoffHours { get; set; }

        public DateTimeOffset CapturedAt { get; set; }

        /*
         * Navigation Properties.
         */

        public Reservation Reservation { get; set; }
            = null!;

        public CancellationPolicy? CancellationPolicy { get; set; }

        public CancellationPolicyVersion?
            CancellationPolicyVersion
        { get; set; }
    }

}