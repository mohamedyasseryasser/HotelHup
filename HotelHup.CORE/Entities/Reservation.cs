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

    public class Reservation:BaseEntity
    {
        [Key, Required]
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

        public string? CancellationPolicySnapshot { get; set; }

        public string? RateSnapshot { get; set; }


        // Navigation

        public Guest Guest { get; set; } = null!;

        public Property Property { get; set; } = null!;

        public ICollection<ReservationRoom> ReservationRooms { get; set; }
            = new List<ReservationRoom>();

        public Folio? Folio { get; set; }
    }
}
