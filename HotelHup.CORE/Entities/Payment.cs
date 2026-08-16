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
    public class Payment
    {
        [Key, Required]
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string ReceivedById { get; set; }
        [ForeignKey(nameof(ReceivedById))]
        public User ReceivedBy { get; set; } = null!;
    }
}
