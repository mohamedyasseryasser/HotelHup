using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.property
{
 


    public sealed class UpdateDepositPolicyRequest
      : CreateDepositPolicyRequest
    {
        public string? Reason { get; init; }
    }


    public sealed class DepositPolicyResponse
    {
        public int Id { get; init; }

        public int PropertyId { get; init; }

        public bool IsActive { get; init; }

        public int CurrentVersion { get; init; }
        public string RowVersion { get; init; } = string.Empty;

        public IReadOnlyList<DepositPolicyVersionResponse> Versions
        {
            get;
            init;
        } = Array.Empty<DepositPolicyVersionResponse>();
    }

    public sealed class DepositPolicyVersionResponse
    {
        public int Id { get; init; }

        public int DepositId { get; init; }

        public int Version { get; init; }

        public string Name { get; init; } = string.Empty;

        public DepositType Type { get; init; }

        public decimal Amount { get; init; }

        public decimal Percentage { get; init; }

        public DateTimeOffset ValidFrom { get; init; }

        public DateTimeOffset? ValidTo { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public string? CreatedBy { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public string? UpdatedBy { get; init; }
    }



}
