using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{

    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default);
        Task<IReadOnlyList<Payment>> GetByFolioIdAsync(int folioId, CancellationToken ct = default);
        Task<Payment?> GetByIdempotencyKeyAsync(string key, bool tracking, CancellationToken ct = default);
        Task<Payment?> GetByExternalIdAsync(string externalId, bool tracking, CancellationToken ct = default);
        Task<Folio?> GetFolioForUpdateAsync(int folioId, CancellationToken ct = default);
        Task AddAsync(Payment payment, CancellationToken ct = default);
        Task AddAsync(Refund refund, CancellationToken ct = default);
        Task AddAuditAsync(AuditLog audit, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    }
}
