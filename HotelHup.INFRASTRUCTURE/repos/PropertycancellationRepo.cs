using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{
    public class PropertycancellationRepo:IPropertyCancellationRepo
    {
        public hotelhupContext _context { get; }

        public PropertycancellationRepo(hotelhupContext context)
        {
            _context = context;
        }
        public async Task<bool>
    HasActiveReservationsUsingCancellationPolicyAsync(
        int propertyId,
        int policyId,
        CancellationToken cancellationToken = default)
        {
            return await _context.Reservations
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.CancellationPolicyId == policyId &&
                        (
                            x.Status == ReservationStatus.Confirmed ||
                            x.Status == ReservationStatus.CheckedIn
                        ),
                    cancellationToken);
        }
        public async Task<IReadOnlyList<CancellationPolicy>> GetCancellationPoliciesAsync(
           int propertyId, CancellationToken ct = default)
        {
            return await _context.CancellationPolicies
         .AsNoTracking()
         .Where(x => x.PropertyId == propertyId)
         .Include(x => x.Versions)
         .OrderByDescending(x => x.CurrentVersion)
         .ThenBy(x => x.Name)
         .ToListAsync(ct);
        }
        public async Task<bool> CancellationVersionOverlapsAsync(
    int cancellationPolicyId,
    DateTimeOffset validFrom,
    DateTimeOffset? validTo,
    CancellationToken cancellationToken = default)
        {
            var requestedTo = validTo ?? DateTimeOffset.MaxValue;

            return await _context.cancellationPolicyVersions
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.CancellationPolicyId == cancellationPolicyId &&
                        x.ValidFrom < requestedTo &&
                        (x.ValidTo ?? DateTimeOffset.MaxValue) > validFrom,
                    cancellationToken);
        }
        public async Task<int>
    CountActiveCancellationPoliciesAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
        {
            return await _context.CancellationPolicies
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Status == PolicyStatus.Active,
                    cancellationToken);
        }
        public async Task AddCancellationPolicyWithAuditAsync(
    CancellationPolicy policy,
    AddAuditLogDto audit,
    CancellationToken ct = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(ct);

            try
            {
                await _context.CancellationPolicies.AddAsync(
                    policy,
                    ct);
                await AddAuditLogAsync(audit, ct);

                await _context.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        public async Task<CancellationPolicy?> GetCancellationPolicyAsync(
            int propertyId, int policyId, CancellationToken ct = default)
        {
            return await _context.CancellationPolicies
                .Where(x => x.PropertyId == propertyId && x.Id == policyId)
                .Include(x => x.Versions)
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);
        }
     
         
        public async Task<bool> CancellationPolicyNameExistsAsync(
    int propertyId,
    string normalizedName,
    CancellationToken cancellationToken = default)
        {
            return await _context.CancellationPolicies
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Name.ToUpper() == normalizedName,
                    cancellationToken);
        }

        public async Task<CancellationPolicy?>
         GetTrackedCancellationPolicyAsync(
             int propertyId,
             int policyId,
             CancellationToken cancellationToken = default)
        {
            return await _context.CancellationPolicies
                .Where(x =>
                    x.PropertyId == propertyId &&
                    x.Id == policyId)
                .Include(x => x.Versions)
                .SingleOrDefaultAsync(
                    cancellationToken);
        }


        public async Task<int> GetNextCancellationVersionAsync(
            int propertyId, int? policyId = null, CancellationToken ct = default)
        {
            var query = _context.cancellationPolicyVersions.AsQueryable();
            if (policyId.HasValue)
                query = query.Where(x => x.CancellationPolicyId == policyId.Value);
            else
                query = query.Where(x => x.CancellationPolicy.PropertyId == propertyId);
            var max = await query.MaxAsync(x => (int?)x.Version, ct);
            return (max ?? 0) + 1;
        }

       
      

        public Task AddAuditLogAsync(
            AddAuditLogDto dto,
            CancellationToken ct = default)
        {
            var auditLog = new AuditLog
            {
                UserId = dto.userid,
                PropertyId = dto.PropertyId,

                EntityName =
                    string.IsNullOrWhiteSpace(dto.TargetEntity)
                        ? "Unknown"
                        : dto.TargetEntity,

                EntityId = dto.TargetEntityId,
                Action = dto.Action,

                OldValues =
                    dto.OldValues is null
                        ? null
                        : JsonSerializer.Serialize(dto.OldValues),

                NewValues =
                    dto.NewValues is null
                        ? null
                        : JsonSerializer.Serialize(dto.NewValues),

                Reason = dto.Reason,
                CorrelationId = dto.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = dto.userid
            };

            _context.AuditLogs.Add(auditLog);

            return Task.CompletedTask;
        }
 
 
    }
}
