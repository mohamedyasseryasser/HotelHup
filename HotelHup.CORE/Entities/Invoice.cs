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
    public class Invoice
    {
        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;
        public int ReservationId { get; set; } // Unique: حجز واحد = فاتورة واحدة بس
        [ForeignKey(nameof(ReservationId))]
        public Reservation Reservation { get; set; } = null!;

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

        // Navigation
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
