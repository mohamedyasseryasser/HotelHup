using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IPropertyDepositRepo
    {
        Task<IReadOnlyList<DepositPolicy>> GetDepositPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<DepositPolicy?> GetDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
        Task<DepositPolicy?> GetTrackedDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
        Task<int> GetNextDepositVersionAsync(int propertyId, int? policyId = null, CancellationToken cancellationToken = default);
        Task AddDepositPolicyWithAuditAsync(
              DepositPolicy policy,
              AddAuditLogDto auditLog,
              CancellationToken cancellationToken = default);
        Task<bool> DepositVersionOverlapsAsync(
int depositPolicyId,
DateTimeOffset validFrom,
DateTimeOffset? validTo,
int? excludedVersionId = null,
CancellationToken cancellationToken = default);


        Task SetDepositPolicyStatusWithAuditAsync(
    DepositPolicy policy,
    AddAuditLogDto auditLog,
    CancellationToken cancellationToken = default);

    }
}
