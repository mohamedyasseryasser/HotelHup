using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydepositdtos
{
    public class CreateDepositPolicyRequest
    {
        [Required, StringLength(150)]
        public string Name { get; init; } = string.Empty;

        public DepositType Type { get; init; }

        public decimal Amount { get; init; }

        public decimal Percentage { get; init; }

        public DateTimeOffset ValidFrom { get; init; }
            = DateTimeOffset.UtcNow;

        public DateTimeOffset? ValidTo { get; init; }

        public string? IfMatch { get; init; }
    }
}
