using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property.propertycancellationdtos
{
    public sealed class CancellationPolicyResponse
    {
        public int Id { get; init; }
        public string RowVersion { get; init; } = string.Empty;

        public int PropertyId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public PolicyStatus Status { get; init; }

        public int CurrentVersion { get; init; }

        public IReadOnlyList<CancellationPolicyVersionResponse> Versions
        {
            get;
            init;
        } = Array.Empty<CancellationPolicyVersionResponse>();
    }

}
