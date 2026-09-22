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

    public class Reservation : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuestId { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }
            = ReservationStatus.Pending;

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 20)]
        public int Adults { get; set; }

        [Range(0, 20)]
        public int Children { get; set; }

        public BookingSource Source { get; set; }
 
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
        public int? CancellationPolicyId { get; set; }

        public int? CancellationPolicyVersionId { get; set; }

       

        public int? DepositPolicyId { get; set; }

        public int? DepositPolicyVersionId { get; set; }
        /*
         * Navigation Properties
         */
        public CancellationPolicy? CancellationPolicy { get; set; }

        public CancellationPolicyVersion? CancellationPolicyVersion { get; set; }

        public DepositPolicy? DepositPolicy { get; set; }

        public DepositPolicyVersion? DepositPolicyVersion { get; set; }

        public Guest Guest { get; set; } = null!;
        public Property Property { get; set; } = null!;
          
        public ICollection<ReservationRoom> ReservationRooms { get; set; }= new List<ReservationRoom>();
 
        public Folio? Folio { get; set; }
        public ICollection<ReservationStatusHistory> StatusHistory{ get; set; } = new List<ReservationStatusHistory>();
        public ICollection<ReservationTaxSnapshot> ReservationTaxSnapshots{ get; set; }= new List<ReservationTaxSnapshot>();
          public ReservationCancellationPolicySnapshot? CancellationPolicySnapshot{ get; set; }
          public ReservationDepositPolicySnapshot?  DepositPolicySnapshot  { get; set; }
    }

}
