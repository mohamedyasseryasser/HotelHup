using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{
    public interface ICancellationPolicyService
    {
        Task<ResponseStatus<IReadOnlyList<CancellationPolicyResponse>>> GetCancellationPoliciesAsync(User actor, int propertyId, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> GetCancellationPolicyAsync(User actor, int propertyId, int id, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>> CreateCancellationPolicyAsync(User actor, int propertyId, CreateCancellationPolicyRequest request, CancellationToken ct = default);
        Task<ResponseStatus<CancellationPolicyResponse>>
            UpdateCancellationPolicyAsync(
                User actor,
                int propertyId,
                int policyId,
                UpdateCancellationPolicyRequest request,
                string ifMatch,
                CancellationToken ct = default);

        Task<ResponseStatus<CancellationPolicyResponse>>
            SetCancellationPolicyStatusAsync(
                User actor,
                int propertyId,
                int policyId,
                bool active,
                ChangeCancellationPolicyStatusRequest request,
                CancellationToken ct = default);
    }
}
