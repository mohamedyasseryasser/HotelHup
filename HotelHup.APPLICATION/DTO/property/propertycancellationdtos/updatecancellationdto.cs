using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertycancellationdtos
{
    public sealed class UpdateCancellationPolicyRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; init; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; init; }

        [Required]
        public DateTimeOffset ValidFrom { get; init; }

        public DateTimeOffset? ValidTo { get; init; }

        [Range(0, int.MaxValue)]
        public int FreeCancellationHours { get; init; }

        [Range(typeof(decimal), "0", "100")]
        public decimal CancellationFeePercentage { get; init; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal FixedCancellationFee { get; init; }

        public bool IsNonRefundable { get; init; }

        [Range(0, int.MaxValue)]
        public int CutoffHours { get; init; }

        [Required]
        public string Rules { get; init; } = "{}";

        [Required]
        [StringLength(500, MinimumLength = 2)]
        public string Reason { get; init; } = string.Empty;
    }


}
