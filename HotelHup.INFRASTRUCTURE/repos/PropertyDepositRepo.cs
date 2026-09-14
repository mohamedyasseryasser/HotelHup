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
    public class PropertyDepositRepo:IPropertyDepositRepo
    {
        public hotelhupContext _context { get; }

        public PropertyDepositRepo(hotelhupContext context)
        {
            _context = context;
        }
        public async Task<IReadOnlyList<DepositPolicy>> GetDepositPoliciesAsync(
         int propertyId, CancellationToken ct = default)
        {
            return await _context.DepositPolicies
                .Where(x => x.PropertyId == propertyId)
                .Include(x => x.Versions)
                .OrderByDescending(x => x.CurrentVersion)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<DepositPolicy?> GetDepositPolicyAsync(
            int propertyId, int policyId, CancellationToken ct = default)
        {
            return await _context.DepositPolicies
                .Where(x => x.PropertyId == propertyId && x.Id == policyId)
                .Include(x => x.Versions)
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);
        }
        public async Task AddDepositPolicyWithAuditAsync(
    DepositPolicy policy,
    AddAuditLogDto dto,
    CancellationToken ct = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(ct);

            try
            {
                await _context.DepositPolicies.AddAsync(
                    policy,
                    ct);


                await _context.SaveChangesAsync(ct);

                var auditLog = new AuditLog
                {
                    UserId = dto.userid,
                    PropertyId = dto.PropertyId,

                    EntityName =
                        string.IsNullOrWhiteSpace(dto.TargetEntity)
                            ? "Unknown"
                            : dto.TargetEntity,

                    EntityId =
                        dto.TargetEntityId == "pending"
                            ? policy.Id.ToString()
                            : dto.TargetEntityId,

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

                await _context.AuditLogs.AddAsync(
                    auditLog,
                    ct);

                await _context.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
        public async Task SetDepositPolicyStatusWithAuditAsync(
    DepositPolicy policy,
    AddAuditLogDto dto,
    CancellationToken ct = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(ct);

            try
            {
                _context.DepositPolicies.Update(policy);

                await _context.SaveChangesAsync(ct);

                _context.AuditLogs.Add(
                    new AuditLog
                    {
                        UserId =
                            dto.userid,

                        PropertyId =
                            dto.PropertyId,

                        EntityName =
                            string.IsNullOrWhiteSpace(dto.TargetEntity)
                                ? "DepositPolicy"
                                : dto.TargetEntity,

                        EntityId =
                            dto.TargetEntityId,

                        Action =
                            dto.Action,

                        OldValues =
                            dto.OldValues is null
                                ? null
                                : JsonSerializer.Serialize(dto.OldValues),

                        NewValues =
                            dto.NewValues is null
                                ? null
                                : JsonSerializer.Serialize(dto.NewValues),

                        Reason =
                            dto.Reason,

                        CorrelationId =
                            dto.CorrelationId,

                        CreatedAt =
                            DateTimeOffset.UtcNow,

                        CreatedBy =
                            dto.userid
                    });

                await _context.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }


        public async Task<DepositPolicy?> GetTrackedDepositPolicyAsync(
            int propertyId, int policyId, CancellationToken ct = default)
        {
            return await _context.DepositPolicies
                .Where(x => x.PropertyId == propertyId && x.Id == policyId)
                .Include(x => x.Versions)
                .SingleOrDefaultAsync(ct);
        }

        public async Task<int> GetNextDepositVersionAsync(
            int propertyId, int? policyId = null, CancellationToken ct = default)
        {
            var query = _context.depositPolicyVersions.AsQueryable();
            if (policyId.HasValue)
                query = query.Where(x => x.DepositId == policyId.Value);
            else
                query = query.Where(x => x.Deposit.PropertyId == propertyId);
            var max = await query.MaxAsync(x => (int?)x.Version, ct);
            return (max ?? 0) + 1;
        }
        public async Task<bool> DepositVersionOverlapsAsync(
int depositPolicyId,
DateTimeOffset validFrom,
DateTimeOffset? validTo,
int? excludedVersionId = null,
CancellationToken ct = default)
        {
            var requestedFrom =
                validFrom.ToUniversalTime();

            var requestedTo =
                (validTo ?? DateTimeOffset.MaxValue)
                .ToUniversalTime();

            return await _context.depositPolicyVersions
                .AsNoTracking()
                .Where(x =>
                    x.DepositId == depositPolicyId)
                .Where(x =>
                    !excludedVersionId.HasValue ||
                    x.Id != excludedVersionId.Value)
                .AnyAsync(
                    x =>
                        x.ValidFrom < requestedTo &&
                        (x.ValidTo ?? DateTimeOffset.MaxValue)
                            > requestedFrom,
                    ct);
        }
    }
}
