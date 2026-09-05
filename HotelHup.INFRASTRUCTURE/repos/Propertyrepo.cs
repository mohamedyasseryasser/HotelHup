using System.Text.Json;

using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;

using Microsoft.EntityFrameworkCore;

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

        public async Task<IReadOnlyList<Property>> GetListAsync(
            string? search,
            PropertyStatus? status,
            string sortBy,
            bool descending,
            int skip,
            int take,
            CancellationToken ct = default)
        {
            var query = BuildQuery(
                search,
                status);

            query = (sortBy ?? "name")
                .Trim()
                .ToLowerInvariant()
                . switch
            {
                "code" =>
                    descending
                        ? query.OrderByDescending(x => x.Code)
                        : query.OrderBy(x => x.Code),

                "createdat" =>
                    descending
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt),

                "status" =>
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

        public async Task<IReadOnlyList<CancellationPolicy>>
            GetCancellationPoliciesAsync(
                int propertyId,
                CancellationToken ct = default)
        {
            var policies = await _context.CancellationPolicies
                .Where(x => x.PropertyId == propertyId)
                .OrderByDescending(x => x.Version)
                .AsNoTracking()
                .ToListAsync(ct);

            return policies;
        }

        public async Task<CancellationPolicy?> GetCancellationPolicyAsync(
            int propertyId,
            int policyId,
            CancellationToken ct = default)
        {
            return await _context.CancellationPolicies
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == policyId,
                    ct);
        }

        public async Task<CancellationPolicy?>
            GetTrackedCancellationPolicyAsync(
                int propertyId,
                int policyId,
                CancellationToken ct = default)
        {
            return await _context.CancellationPolicies
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == policyId,
                    ct);
        }

        public async Task<int> GetNextCancellationVersionAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            var maxVersion = await _context.CancellationPolicies
                .Where(x => x.PropertyId == propertyId)
                .MaxAsync(
                    x => (int?)x.Version,
                    ct);

            return (maxVersion ?? 0) + 1;
        }

        public async Task<IReadOnlyList<DepositPolicy>>
            GetDepositPoliciesAsync(
                int propertyId,
                CancellationToken ct = default)
        {
            var policies = await _context.DepositPolicies
                .Where(x => x.PropertyId == propertyId)
                .OrderByDescending(x => x.Version)
                .AsNoTracking()
                .ToListAsync(ct);

            return policies;
        }

        public async Task<DepositPolicy?> GetDepositPolicyAsync(
            int propertyId,
            int policyId,
            CancellationToken ct = default)
        {
            return await _context.DepositPolicies
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == policyId,
                    ct);
        }

        public async Task<DepositPolicy?>
            GetTrackedDepositPolicyAsync(
                int propertyId,
                int policyId,
                CancellationToken ct = default)
        {
            return await _context.DepositPolicies
                .SingleOrDefaultAsync(
                    x =>
                        x.PropertyId == propertyId &&
                        x.Id == policyId,
                    ct);
        }

        public async Task<int> GetNextDepositVersionAsync(
            int propertyId,
            CancellationToken ct = default)
        {
            var maxVersion = await _context.DepositPolicies
                .Where(x => x.PropertyId == propertyId)
                .MaxAsync(
                    x => (int?)x.Version,
                    ct);

            return (maxVersion ?? 0) + 1;
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
    }
}