using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertytaxdto
{
    public sealed class CreateTaxRequest
    {
        [Required, StringLength(150)] public string Name { get; init; } = string.Empty;
        [Required, StringLength(50)] public string Code { get; init; } = string.Empty;
        [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal Rate { get; init; }
        [Required]
        public TaxType Type { get; init; }
        public bool IsInclusive { get; init; }
        public DateTimeOffset ValidFrom { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ValidTo { get; init; }
    }
}
