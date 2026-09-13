using HotelHup.APPLICATION.DTO.property.propertysettingdto;
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertydto
{
    public sealed class PropertyResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public PropertyStatus Status { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public string? UpdatedBy { get; init; }
        public string RowVersion { get; init; } = string.Empty;
        public int ActiveTaxes { get; init; }
        public int ActiveCancellationPolicies { get; init; }
        public int ActiveDepositPolicies { get; init; }
        public PropertySettingsResponse? SettingsResponse { get; init; }
    }
}
