using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetByIdAndPropertyAsync(int id, int? propertyId, bool tracking, CancellationToken ct = default);
        Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetPagedAsync(int? propertyId, DateTimeOffset? from, DateTimeOffset? to, string? category, HotelHup.CORE.Enums.PaymentMethod? method, HotelHup.CORE.Enums.ExpenseStatus? status, string? vendor, int page, int size, string? sortBy, bool descending, CancellationToken ct = default);
        Task AddAsync(Expense expense, AuditLog audit, CancellationToken ct = default);
        Task AddAuditAsync(AuditLog audit, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    }
}
