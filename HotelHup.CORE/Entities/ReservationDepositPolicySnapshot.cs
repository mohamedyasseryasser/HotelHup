using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelHup.CORE.Entities
{

    public class ReservationDepositPolicySnapshot : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required,ForeignKey(nameof(Reservation))]
        public int ReservationId { get; set; }

        public int? DepositPolicyId { get; set; }

        public int? DepositPolicyVersionId { get; set; }

        [Required, MaxLength(150)]
        public string PolicyName { get; set; } = string.Empty;

        public DepositType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RequiredDepositAmount { get; set; }

        public DateTimeOffset CapturedAt { get; set; }

        /*
         * Navigation Properties.
         */

        public Reservation Reservation { get; set; }
            = null!;

        public DepositPolicy? DepositPolicy { get; set; }

        public DepositPolicyVersion? DepositPolicyVersion { get; set; }
    }

}