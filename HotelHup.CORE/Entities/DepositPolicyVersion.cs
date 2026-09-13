using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.CORE.Entities
{
    public class DepositPolicyVersion:BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int DepositId { get; set; }

        public int Version { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public DepositType Type { get; set; }

        public decimal Amount { get; set; }

        public decimal Percentage { get; set; }

        public DateTimeOffset ValidFrom { get; set; }

        public DateTimeOffset? ValidTo { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        public DepositPolicy Deposit { get; set; } = null!;
    }
}
