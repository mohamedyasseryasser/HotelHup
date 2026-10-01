 
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelHup.INFRASTRUCTURE.repos;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly hotelhupContext _context;

    public ExpenseRepository(hotelhupContext context)            
    {
        _context = context;
    }

    public Task<Expense?> GetByIdAndPropertyAsync(
        int id,
        int? propertyId,
        bool tracking,
        CancellationToken ct = default)
    {
        IQueryable<Expense> query = _context.Expenses
            .Where(x =>
                x.Id == id &&
                (!propertyId.HasValue ||
                 x.PropertyId == propertyId.Value));

        return (tracking
                ? query
                : query.AsNoTracking())
            .SingleOrDefaultAsync(ct);
    }

    public async Task<(
        IReadOnlyList<Expense> Items,
        int TotalCount)> GetPagedAsync(
        int? propertyId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? category,
        PaymentMethod? method,
        ExpenseStatus? status,
        string? vendor,
        int page,
        int size,
        string? sortBy,
        bool descending,
        CancellationToken ct = default)
    {
        IQueryable<Expense> query =
            _context.Expenses.AsNoTracking();

        if (propertyId.HasValue)
        {
            query = query.Where(
                x => x.PropertyId == propertyId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(
                x => x.ExpenseDate >= from.Value);
        }

        if (to.HasValue)                                                           
        {
            query = query.Where(
                x => x.ExpenseDate <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(
                x => x.Category == category);
        }

        if (method.HasValue)
        {
            query = query.Where(
                x => x.PaymentMethod == method.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(                                           
                x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(vendor))
        {
            query = query.Where(
                x =>
                    x.VendorName != null &&
                    x.VendorName.Contains(vendor));
        }

        var total = await query.CountAsync(ct);

        query = (sortBy?.ToLowerInvariant()) switch
        {
            "amount" =>
                descending
                    ? query.OrderByDescending(x => x.Amount)
                    : query.OrderBy(x => x.Amount),

            "category" =>
                descending
                    ? query.OrderByDescending(x => x.Category)
                    : query.OrderBy(x => x.Category),

            _ =>
                descending
                    ? query
                        .OrderByDescending(x => x.ExpenseDate)
                        .ThenByDescending(x => x.Id)
                    : query
                        .OrderBy(x => x.ExpenseDate)
                        .ThenBy(x => x.Id)
        };

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(
        Expense expense,
        AuditLog audit,
        CancellationToken ct = default)
    {
        await _context.Expenses.AddAsync(
            expense,
            ct);

        await _context.AuditLogs.AddAsync(
            audit,
            ct);
    }

    public Task AddAuditAsync(
        AuditLog audit,
        CancellationToken ct = default)
    {
        return _context.AuditLogs
            .AddAsync(audit, ct)
            .AsTask();
    }

    public Task SaveChangesAsync(
        CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task<T> ExecuteTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        await using var tx =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                ct);

        try
        {
            var result = await operation();

            await tx.CommitAsync(ct);

            return result;
        }
        catch
        {
            await tx.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}
