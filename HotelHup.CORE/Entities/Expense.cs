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
    public class Expense : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey(nameof(Property))]
        public int PropertyId { get; set; }
        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [Required, MaxLength(3)]
        public string Currency { get; set; } = "USD";
        public DateTimeOffset ExpenseDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        [MaxLength(200)] public string? VendorName { get; set; }
        [MaxLength(100)] public string? ReferenceNumber { get; set; }
        [MaxLength(2000)] public string? Notes { get; set; }
        public ExpenseStatus Status { get; set; } = ExpenseStatus.Posted;
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Property Property { get; set; } = null!;
    }
}
