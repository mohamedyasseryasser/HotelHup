using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace HotelHup.CORE.Entities
{
    public class Payment:BaseEntity
    {
        [Key, Required]
        public int Id { get; set; }
        public int FolioId { get; set; }

        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; }

        public decimal Amount { get; set; }

        public string? ExternalId { get; set; }

        public string? IdempotencyKey { get; set; }

        public DateTime? PaidAt { get; set; }


        // Navigation

        public Folio Folio { get; set; } = null!;

        public ICollection<Refund> Refunds { get; set; }
            = new List<Refund>();
    }
}
