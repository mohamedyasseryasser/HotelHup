using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertycancellationdtos
{
    public sealed class CancellationPolicyVersionResponse
    {
        public int Id { get; init; }

        public int CancellationPolicyId { get; init; }

        public int Version { get; init; }

        public DateTimeOffset ValidFrom { get; init; }

        public DateTimeOffset? ValidTo { get; init; }

        public string Rules { get; init; } = "{}";

        public int FreeCancellationHours { get; init; }

        public decimal CancellationFeePercentage { get; init; }

        public decimal FixedCancellationFee { get; init; }

        public bool IsNonRefundable { get; init; }

        public int CutoffHours { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public string? CreatedBy { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public string? UpdatedBy { get; init; }
    }
}
