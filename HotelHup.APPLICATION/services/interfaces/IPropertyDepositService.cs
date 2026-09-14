using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface IPropertyDepositService
    {
        Task<ResponseStatus<IReadOnlyList<DepositPolicyResponse>>> GetDepositPoliciesAsync(User actor, int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> GetDepositPolicyAsync(User actor, int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> CreateDepositPolicyAsync(User actor, int propertyId, CreateDepositPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> UpdateDepositPolicyAsync(User actor, int propertyId, int id, UpdateDepositPolicyRequest request, string ifmathc, CancellationToken ct = default);
        Task<ResponseStatus<DepositPolicyResponse>> SetDepositPolicyStatusAsync(
            User actor,
            int propertyId,
            int id,
            bool active,
            string? reason,
            string? ifMatch,
            CancellationToken ct = default);
    }
}
