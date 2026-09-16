
using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IRatePlanRepository
    {
        Task<List<RatePlan>> GetAllAsync(int propertyid,CancellationToken ct = default);
        Task<RatePlan?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default);
        Task<List<RatePlanVersion>> GetVersionsAsync(int ratePlanId, CancellationToken ct = default);
        Task<RatePlanVersion?> GetVersionAsync(int ratePlanId, int versionNumber, CancellationToken ct = default);
        Task<RatePlanVersion?> GetLatestVersionAsync(int ratePlanId, CancellationToken ct = default);
   
        Task PersistAsync(RatePlan plan, RatePlanVersion? version, AddAuditLogDto audit, CancellationToken ct = default);
    }
}
