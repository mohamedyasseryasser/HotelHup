using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using System.Threading;

namespace HotelHup.INFRASTRUCTURE.repos
{
    public sealed class PropertyRepository : IPropertyRepository
    {
        private readonly hotelhupContext _context;

        public PropertyRepository(hotelhupContext context)
        {
            _context = context;
        }

        public async Task<Property?> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            return await _context.Properties
        .AsNoTracking()
        .Include(x => x.Settings)
        .Include(x => x.Taxes)
        .Include(x => x.CancellationPolicies)
        .Include(x => x.DepositPolicies)
        .SingleOrDefaultAsync(
            x => x.ID == id,
            ct);
        }

        public async Task<Property?> GetTrackedByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            return await _context.Properties
                .Include(x => x.Settings)
                .SingleOrDefaultAsync(
                    x => x.ID == id,
                    ct);
        }
        public async Task<IDbContextTransaction>
     BeginTransactionAsync(
         CancellationToken cancellationToken = default)
        {
            return await _context.Database
                .BeginTransactionAsync(
                    cancellationToken);
        }

        public async Task<IReadOnlyList<Property>> GetListAsync(
       string? search,
       PropertyStatus? status,
       PropertySortBy sortBy,
       bool descending,
       int skip,
       int take,
       CancellationToken ct = default)
        {
            var query = BuildQuery(
                search,
                status);

            query = sortBy switch
            {
                PropertySortBy.Code =>
                    descending
                        ? query.OrderByDescending(x => x.Code)
                        : query.OrderBy(x => x.Code),

                PropertySortBy.CreatedAt =>
                    descending
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt),

                PropertySortBy.Status =>
                    descending
                        ? query.OrderByDescending(x => x.Status)
                        : query.OrderBy(x => x.Status),

                _ =>
                    descending
                        ? query.OrderByDescending(x => x.Name)
                        : query.OrderBy(x => x.Name)
            };

            return await query
                .Skip(Math.Max(0, skip))
                .Take(Math.Max(1, take))
                .AsNoTracking()
                .ToListAsync(ct);
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
        public async Task<int> CountAsync(
            string? search,
            PropertyStatus? status,
            CancellationToken ct = default)
        {
            return await BuildQuery(
                search,
                status)
                .CountAsync(ct);
        }

        public async Task<bool> CodeExistsAsync(
            string code,
            int? excludingId = null,
            CancellationToken ct = default)
        {
            return await _context.Properties
                .AnyAsync(
                    x =>
                        x.Code == code &&
                        (
                            !excludingId.HasValue ||
                            x.ID != excludingId.Value
                        ),
                    ct);
        }
        public async Task<Property> CreateAsync(
    Property property,
    PropertySettings settings,
    AddAuditLogDto auditLog,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(auditLog);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                property.Settings = settings;
                settings.Property = property;

                _context.Properties.Add(property);

                 await _context.SaveChangesAsync(cancellationToken);

                var persistedAudit = new AddAuditLogDto
                {
                    userid = auditLog.userid,
                    PropertyId = property.ID,
                    TargetEntity = auditLog.TargetEntity,
                    TargetEntityId = property.ID.ToString(),
                    Action = auditLog.Action,
                    OldValues = auditLog.OldValues,
                    NewValues = auditLog.NewValues,
                    Reason = auditLog.Reason,
                    CorrelationId = auditLog.CorrelationId
                };

                _context.AuditLogs.Add(ToAuditLog(persistedAudit));

                 await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return property;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        public async Task<bool> UpdateAsync(
    Property property,
    AddAuditLogDto auditLog,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(auditLog);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var persistedAudit = new AddAuditLogDto
                {
                    userid = auditLog.userid,
                    PropertyId = property.ID,
                    TargetEntity = auditLog.TargetEntity,
                    TargetEntityId = property.ID.ToString(),
                    Action = auditLog.Action,
                    OldValues = auditLog.OldValues,
                    NewValues = auditLog.NewValues,
                    Reason = auditLog.Reason,
                    CorrelationId = auditLog.CorrelationId
                };

                _context.AuditLogs.Add(ToAuditLog(persistedAudit));

                 await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return false;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private static AuditLog ToAuditLog(AddAuditLogDto dto) => new()
        {
            UserId = dto.userid,
            PropertyId = dto.PropertyId,
            EntityName = string.IsNullOrWhiteSpace(dto.TargetEntity)
        ? "Unknown"
        : dto.TargetEntity,
            EntityId = dto.TargetEntityId,
            Action = dto.Action,
            OldValues = dto.OldValues is null
        ? null
        : JsonSerializer.Serialize(dto.OldValues),
            NewValues = dto.NewValues is null
        ? null
        : JsonSerializer.Serialize(dto.NewValues),
            Reason = dto.Reason,
            CorrelationId = dto.CorrelationId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = dto.userid
        };

        public async Task<bool> HasActiveReservationsAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            return await _context.Reservations
                .AnyAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        (
                            x.Status == ReservationStatus.Pending ||
                            x.Status == ReservationStatus.Confirmed ||
                            x.Status == ReservationStatus.CheckedIn
                        ),
                    ct);
        }

        public async Task<bool> HasOpenFoliosAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            return await _context.Folios
                .AnyAsync(
                    x =>
                        x.Reservation.PropertyId == propertyId &&
                        x.Status == FolioStatus.Open,
                    ct);
        }

        public async Task<bool> HasFinancialTransactionsAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            return await _context.Folios
                .AnyAsync(
                    x =>
                        x.Reservation.PropertyId == propertyId &&
                        (
                            x.Total != 0 ||
                            x.Balance != 0 ||
                            x.Items.Any() ||
                            x.Payments.Any()
                        ),
                    ct);
        }

        public async Task<PropertySettings?> GetSettingsAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            return await _context.PropertySettings
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.PropertyId == propertyId,
                    ct);
        }

        public async Task<PropertySettings?> GetTrackedSettingsAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            return await _context.PropertySettings
                .SingleOrDefaultAsync(
                    x => x.PropertyId == propertyId,
                    ct);
        }

        public async Task<IReadOnlyList<Tax>> GetTaxesAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            var taxes = await _context.Taxes
                .Where(x => x.PropertyId == propertyId)
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync(ct);

            return taxes;
        }

        public async Task<Tax?> GetTaxAsync(
            int propertyId,
            int taxId,
            CancellationToken ct = default)
        {
            return await _context.Taxes
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == taxId,
                    ct);
        }

        public async Task<Tax?> GetTrackedTaxAsync(
            int propertyId,
            int taxId,
            CancellationToken ct = default)
        {
            return await _context.Taxes
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == taxId,
                    ct);
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
                await  AddAuditLogAsync(audit,ct);

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
 
        public async Task AddAsync<TEntity>(
            TEntity entity,
            CancellationToken ct = default)
            where TEntity : class
        {
            await _context.AddAsync(
                entity,
                ct);
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

        public async Task SaveChangesAsync(
            CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        private IQueryable<Property> BuildQuery(
            string? search,
            PropertyStatus? status)
        {
            var query = _context.Properties
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();

                query = query.Where(
                    x =>
                        x.Name.Contains(value) ||
                        x.Code.Contains(value));
            }

            if (status.HasValue)
            {
                query = query.Where(
                    x => x.Status == status.Value);
            }

            return query;
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