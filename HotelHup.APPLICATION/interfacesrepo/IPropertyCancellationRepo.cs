using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IPropertyCancellationRepo
    {
         Task AddCancellationPolicyWithAuditAsync(
CancellationPolicy policy,
AddAuditLogDto audit,
CancellationToken ct = default)
;
        Task<int> CountActiveCancellationPoliciesAsync(
        int propertyId,
        CancellationToken cancellationToken = default);
        Task<bool>
   HasActiveReservationsUsingCancellationPolicyAsync(
       int propertyId,
       int policyId,
       CancellationToken cancellationToken = default);
            Task<IReadOnlyList<CancellationPolicy>> GetCancellationPoliciesAsync(int propertyId, CancellationToken cancellationToken = default);
        Task<CancellationPolicy?> GetCancellationPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default);
        Task<CancellationPolicy?>
            GetTrackedCancellationPolicyAsync(
                int propertyId,
                int policyId,
                CancellationToken cancellationToken = default);
        Task<int> GetNextCancellationVersionAsync(int propertyId, int? policyId = null, CancellationToken cancellationToken = default);
          Task AddAuditLogAsync(AddAuditLogDto auditLog, CancellationToken cancellationToken = default);
        Task<bool> CancellationPolicyNameExistsAsync(
    int propertyId,
    string normalizedName,
    CancellationToken cancellationToken = default);
        Task<bool> CancellationVersionOverlapsAsync(
    int cancellationPolicyId,
    DateTimeOffset validFrom,
    DateTimeOffset? validTo,
    CancellationToken cancellationToken = default);
    }
}
