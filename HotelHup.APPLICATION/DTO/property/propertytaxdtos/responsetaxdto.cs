using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertytaxdto
{

    public sealed class TaxResponse
    {
        public int Id { get; init; }

        public int PropertyId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public decimal Rate { get; init; }

        public TaxType Type { get; init; }

        public bool IsInclusive { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset ValidFrom { get; init; }

        public DateTimeOffset? ValidTo { get; init; }

        public string RowVersion { get; init; } = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public string? CreatedBy { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public string? UpdatedBy { get; init; }
    }

}
