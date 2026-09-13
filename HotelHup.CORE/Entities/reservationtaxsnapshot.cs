using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities;

public class ReservationTaxSnapshot : BaseEntity
{
    [Key]
    public int Id { get; set; }

    public int ReservationId { get; set; }

   
    public int TaxId { get; set; }

    public string TaxCode { get; set; } = string.Empty;

    public string TaxName { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public TaxType Type { get; set; }

    public bool IsInclusive { get; set; }

     
    public decimal TaxableAmount { get; set; }

     
    public decimal TaxAmount { get; set; }

     
    public DateTimeOffset TaxValidFrom { get; set; }

    public DateTimeOffset? TaxValidTo { get; set; }

    public Reservation Reservation { get; set; } = null!;

    public Tax Tax { get; set; } = null!;
}
