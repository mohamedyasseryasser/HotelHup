using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.repos
{

    public sealed class PaymentRepository : IPaymentRepository
    {
        private readonly hotelhupContext _context;
        public PaymentRepository(hotelhupContext context) => _context = context;

        public async Task<Payment?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default)
        {
            IQueryable<Payment> query = _context.Payments
                .Include(x => x.Refunds)
                .Include(x => x.Folio).ThenInclude(x => x.Reservation)
                .Include(x => x.Folio).ThenInclude(x => x.Items)
                .Include(x => x.Folio).ThenInclude(x => x.Payments).ThenInclude(x => x.Refunds)
                .Where(x => x.Id == id);
            return  await (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<Payment>> GetByFolioIdAsync(int folioId, CancellationToken ct = default) =>
            await _context.Payments.AsNoTracking().Include(x => x.Refunds)
                .Where(x => x.FolioId == folioId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        public Task<Payment?> GetByIdempotencyKeyAsync(string key, bool tracking, CancellationToken ct = default)
        {
            IQueryable<Payment> query = _context.Payments.Include(x => x.Refunds).Include(x => x.Folio).ThenInclude(x => x.Reservation)
                .Where(x => x.IdempotencyKey == key);
            return (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
        }

        public Task<Payment?> GetByExternalIdAsync(string externalId, bool tracking, CancellationToken ct = default)
        {
            IQueryable<Payment> query = _context.Payments.Include(x => x.Refunds).Include(x => x.Folio).ThenInclude(x => x.Reservation)
                .Where(x => x.ExternalId == externalId);
            return (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
        }

        public Task<Folio?> GetFolioForUpdateAsync(int folioId, CancellationToken ct = default) =>
            _context.Folios.Include(x => x.Reservation).ThenInclude(x => x.Property).ThenInclude(x => x.Settings)
                .Include(x => x.Items).Include(x => x.Payments).ThenInclude(x => x.Refunds)
                .SingleOrDefaultAsync(x => x.id == folioId, ct);

        public Task AddAsync(Payment payment, CancellationToken ct = default) => _context.Payments.AddAsync(payment, ct).AsTask();
        public Task AddAsync(Refund refund, CancellationToken ct = default) => _context.Refunds.AddAsync(refund, ct).AsTask();
        public Task AddAuditAsync(AuditLog audit, CancellationToken ct = default) => _context.AuditLogs.AddAsync(audit, ct).AsTask();
        public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
        {
            if (_context.Database.CurrentTransaction is not null) return await operation();
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
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
