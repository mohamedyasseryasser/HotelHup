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

        public int GuestId { get; set; }

        public int PropertyId { get; set; }

        public ReservationStatus Status { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Adults { get; set; }

        public int Children { get; set; }

        public BookingSource Source { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal FeeAmount { get; set; }

        /*
         * References used at reservation creation time.
         */
        public int? CancellationPolicyId { get; set; }

        public int? CancellationPolicyVersionId { get; set; }

        /*
         * Immutable snapshot captured at reservation creation.
         */
        public string? CancellationPolicySnapshot { get; set; }

        public string? RateSnapshot { get; set; }

        public Guest Guest { get; set; } = null!;

        public Property Property { get; set; } = null!;

        public CancellationPolicy? CancellationPolicy { get; set; }

        public CancellationPolicyVersion?
            CancellationPolicyVersion
        { get; set; }

        public ICollection<ReservationRoom> ReservationRooms { get; set; }
            = new List<ReservationRoom>();
        public ICollection<ReservationTaxSnapshot> ReservationTaxSnapshots { get; set; } = 
            new HashSet<ReservationTaxSnapshot>();

        public Folio? Folio { get; set; }
    }

}
