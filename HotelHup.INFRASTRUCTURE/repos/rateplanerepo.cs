using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class RatePlanRepository : IRatePlanRepository
    {
        private readonly hotelhupContext _context;
        public RatePlanRepository(hotelhupContext context) => _context = context;

        public Task<List<RatePlan>> GetAllAsync(int propertyid,CancellationToken ct = default) =>
            _context.RatePlans.AsNoTracking().Where(p=>p.PropertyId==propertyid)
            .Include(x => x.Versions).OrderBy(x => x.Name).ToListAsync(ct);

        public Task<RatePlan?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default)
        {
            IQueryable<RatePlan> query = _context.RatePlans.Include(x => x.Versions);
            if (!tracking) query = query.AsNoTracking();
            return query.SingleOrDefaultAsync(x => x.Id == id, ct);
        }

        public Task<List<RatePlanVersion>> GetVersionsAsync(int ratePlanId, CancellationToken ct = default) =>
            _context.RatePlansversions.AsNoTracking().Where(x => x.RatePlanId == ratePlanId)
                .OrderByDescending(x => x.VersionNumber).ToListAsync(ct);

        public Task<RatePlanVersion?> GetVersionAsync(int ratePlanId, int versionNumber, CancellationToken ct = default) =>
            _context.RatePlansversions.AsNoTracking().SingleOrDefaultAsync(
                x => x.RatePlanId == ratePlanId && x.VersionNumber == versionNumber, ct);

        public Task<RatePlanVersion?> GetLatestVersionAsync(int ratePlanId, CancellationToken ct = default) =>
            _context.RatePlansversions.AsNoTracking().Where(x => x.RatePlanId == ratePlanId)
                .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(ct);

        public async Task PersistAsync(RatePlan plan, RatePlanVersion? version, AddAuditLogDto audit, CancellationToken ct = default)
        {
            if (_context.Entry(plan).State == EntityState.Detached)
             await   _context.RatePlans.AddAsync(plan);
            if (version is not null && _context.Entry(version).State == EntityState.Detached)
            await    _context.RatePlansversions.AddAsync(version);
            if (_context.Entry(plan).State == EntityState.Modified)
                _context.Entry(plan).Property(x => x.RowVersion).OriginalValue = plan.RowVersion;

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = audit.userid,
                    PropertyId = audit.PropertyId,
                    EntityName = audit.TargetEntity,
                    EntityId = audit.TargetEntityId,
                    Action = audit.Action,
                    OldValues = audit.OldValues is null ? null : JsonSerializer.Serialize(audit.OldValues),
                    NewValues = audit.NewValues is null ? null : JsonSerializer.Serialize(audit.NewValues),
                    Reason = audit.Reason,
                    CorrelationId = audit.CorrelationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = audit.userid
                });
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }

}
