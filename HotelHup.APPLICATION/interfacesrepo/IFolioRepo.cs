using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{

    public interface IFolioRepository
    {
        Task<Service?> GetServiceAsync(
    int serviceId,
    int propertyId,
    CancellationToken ct = default);

        Task<Folio?> GetByIdAsync(int id, bool tracking, CancellationToken ct = default);
        Task<Folio?> GetForUpdateAsync(int id, CancellationToken ct = default);
        Task<(IReadOnlyList<FolioItem> Items, int TotalCount)> GetItemsAsync(int folioId, int pageNumber, int pageSize, CancellationToken ct = default);
        Task<bool> IdempotencyKeyExistsAsync(int folioId, string key, CancellationToken ct = default);
        Task SaveAsync(Folio folio, AuditLog audit, CancellationToken ct = default);
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    }
}
