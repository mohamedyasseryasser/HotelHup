using HotelHup.CORE.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.CORE.Entities
{
    public class Refund:BaseEntity
    {
        [Key,Required]
        public int id {  get; set; }
        public int PaymentId { get; set; }

        public int FolioId { get; set; }

        public PaymentMethod Method { get; set; }

        public decimal Amount { get; set; }

        public RefundStatus Status { get; set; }
        public string? Reason { get; set; }

        public string? ExternalId { get; set; }


        // Navigation

        public Payment Payment { get; set; } = null!;

        public Folio Folio { get; set; } = null!;

    }
}