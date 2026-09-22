using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities;

public class CancellationPolicyVersion : BaseEntity
{
    [Key]
    public int Id { get; set; }

    
    public int CancellationPolicyId { get; set; }

    public int Version { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    [MaxLength(4000)]
    public string Rules { get; set; } = "{}";

    public int FreeCancellationHours { get; set; }

    public decimal CancellationFeePercentage { get; set; }

    public decimal FixedCancellationFee { get; set; }
    public bool IsNonRefundable { get; set; }
    public int CutoffHours { get; set; }
    public ICollection<Reservation> Reservations { get; set; }= new List<Reservation>();
    public CancellationPolicy? CancellationPolicy { get; set; } = null!;
    public ICollection<ReservationCancellationPolicySnapshot> ReservationCancellationPolicySnapshots { get; set; }= 
        new List<ReservationCancellationPolicySnapshot>();

}
