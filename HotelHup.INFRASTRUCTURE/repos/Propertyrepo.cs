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
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{


    public sealed class PropertyRepository : IPropertyRepository
    {
        private readonly hotelhupContext _context;
        public PropertyRepository(hotelhupContext context) => _context = context;

        public Task<Property?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _context.Properties.Include(x => x.Settings).Include(x => x.Taxes)
                .Include(x => x.CancellationPolicies).Include(x => x.DepositPolicies)
                .AsNoTracking().SingleOrDefaultAsync(x => x.ID == id, cancellationToken);

        public async Task<IReadOnlyList<Property>> GetListAsync(string? search, PropertyStatus? status, string sortBy, bool descending, int skip, int take, CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(search, status);
            query = sortBy.ToLowerInvariant() switch
            {
                "code" => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
                "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
                _ => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)
            };
            return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync(cancellationToken);
        }

        public Task<int> CountAsync(string? search, PropertyStatus? status, CancellationToken cancellationToken = default) =>
            BuildQuery(search, status).CountAsync(cancellationToken);

        public Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default) =>
            _context.Properties.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.ID != excludingId.Value), cancellationToken);

        public Task<bool> HasActiveReservationsAsync(int propertyId, CancellationToken cancellationToken = default) =>
            _context.Reservations.AnyAsync(x => x.PropertyId == propertyId && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn), cancellationToken);

        public Task<bool> HasOpenFoliosAsync(int propertyId, CancellationToken cancellationToken = default) =>
            _context.Folios.AnyAsync(x => x.Reservation.PropertyId == propertyId && x.Status == FolioStatus.Open, cancellationToken);

        public Task<bool> HasFinancialTransactionsAsync(int propertyId, CancellationToken cancellationToken = default) =>
            _context.Folios.AnyAsync(x => x.Reservation.PropertyId == propertyId && (x.Total != 0 || x.Balance != 0 || x.Payments.Any()), cancellationToken);

        public Task<PropertySettings?> GetSettingsAsync(int propertyId, CancellationToken cancellationToken = default) =>
            _context.PropertySettings.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId, cancellationToken);

        public async Task<IReadOnlyList<Tax>> GetTaxesAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.Taxes.Where(x => x.PropertyId == propertyId).OrderBy(x => x.Name).AsNoTracking().ToListAsync(cancellationToken);

        public Task<Tax?> GetTaxAsync(int propertyId, int taxId, CancellationToken cancellationToken = default) =>
            _context.Taxes.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == taxId, cancellationToken);

        public async Task<IReadOnlyList<CancellationPolicy>> GetCancellationPoliciesAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.CancellationPolicies.Where(x => x.PropertyId == propertyId).OrderByDescending(x => x.Version).AsNoTracking().ToListAsync(cancellationToken);

        public Task<CancellationPolicy?> GetCancellationPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default) =>
            _context.CancellationPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == policyId, cancellationToken);

        public async Task<IReadOnlyList<DepositPolicy>> GetDepositPoliciesAsync(int propertyId, CancellationToken cancellationToken = default) =>
            await _context.DepositPolicies.Where(x => x.PropertyId == propertyId).OrderByDescending(x => x.Version).AsNoTracking().ToListAsync(cancellationToken);

        public Task<DepositPolicy?> GetDepositPolicyAsync(int propertyId, int policyId, CancellationToken cancellationToken = default) =>
            _context.DepositPolicies.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == policyId, cancellationToken);

        public Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class => _context.AddAsync(entity, cancellationToken).AsTask();

        public Task UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task AddAuditLogAsync(AddAuditLogDto dto, CancellationToken cancellationToken = default)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = dto.UserId,
                PropertyId = dto.PropertyId,
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                Action = dto.Action,
                OldValues = dto.OldValues is null ? null : JsonSerializer.Serialize(dto.OldValues),
                NewValues = dto.NewValues is null ? null : JsonSerializer.Serialize(dto.NewValues),
                Reason = dto.Reason,
                CorrelationId = dto.CorrelationId,
                IpAddress = dto.IpAddress,
                ClientContext = dto.ClientContext,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = dto.UserId
            });
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

        private IQueryable<Property> BuildQuery(string? search, PropertyStatus? status)
        {
            var query = _context.Properties.Include(x => x.Settings).Include(x => x.Taxes)
                .Include(x => x.CancellationPolicies).Include(x => x.DepositPolicies).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(x => x.Name.Contains(value) || x.Code.Contains(value));
            }
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);
            return query;
        }
    }
}