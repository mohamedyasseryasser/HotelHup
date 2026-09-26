using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class FolioRepository : IFolioRepository
    {
        private readonly hotelhupContext _context;
        public FolioRepository(hotelhupContext context) => _context = context;

        public async Task<Folio?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default)
        {
            IQueryable<Folio> query = _context.Folios
                .Include(x => x.Reservation)
                .Include(x => x.Items.OrderByDescending(i => i.PostedAt))
                .Include(x => x.Payments).ThenInclude(x => x.Refunds)
                .Where(x => x.id == id);
            if (!tracking) query = query.AsNoTracking();
            return await query.SingleOrDefaultAsync(ct);
        }
        public Task<Service?> GetServiceAsync(
    int serviceId,
    int propertyId,
    CancellationToken ct = default)
        {
            return _context.Services
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.id == serviceId &&
                         x.propertyid == propertyId,
                    ct);
        }

        public Task<Folio?> GetForUpdateAsync(int id, CancellationToken ct = default) => GetByIdAsync(id, true, ct);

        public async Task<(IReadOnlyList<FolioItem> Items, int TotalCount)> GetItemsAsync(
            int folioId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var query = _context.FolioItems.AsNoTracking()
                .Where(x => x.FolioId == folioId)
                .OrderByDescending(x => x.PostedAt).ThenByDescending(x => x.id);
            var total = await query.CountAsync(ct);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }

        public Task<bool> IdempotencyKeyExistsAsync(int folioId, string key, CancellationToken ct = default) =>
            _context.FolioItems.AsNoTracking().AnyAsync(x => x.FolioId == folioId && x.SourceReference == key, ct);

        public async Task SaveAsync(Folio folio, AuditLog audit, CancellationToken ct = default)
        {
            // Folio is loaded as a tracked aggregate for write operations; keep
            // already-posted financial rows unchanged and let EF detect only the
            // intended state change/new item.
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
        {
            if (_context.Database.CurrentTransaction is not null) return await operation();
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }

}
