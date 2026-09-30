using System.Data;
using HotelHup.APPLICATION.DTO.maintenance;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.INFRASTRUCTURE.repos;

public sealed class MaintenanceRepository : IMaintenanceRepository
{
    private readonly hotelhupContext _context;

    public MaintenanceRepository(hotelhupContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<MaintenanceTicket> Items, int TotalCount)> GetPagedAsync(
        MaintenanceListRequest request,
        CancellationToken ct = default)
    {
        var query = _context.MaintenanceTickets
            .AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.Assignee)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.Room.property_id == request.PropertyId.Value);
        }

        if (request.RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == request.RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AssigneeId))
        {
            query = query.Where(x => x.AssigneeId == request.AssigneeId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == request.Priority.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<MaintenanceTicket?> GetByIdAndPropertyAsync(
        int maintenanceId,
        int propertyId,
        bool tracking,
        CancellationToken ct = default)
    {
        IQueryable<MaintenanceTicket> query = _context.MaintenanceTickets
            .Include(x => x.Room)
            .Include(x => x.Assignee)
            .Where(x => x.id == maintenanceId && x.Room.property_id == propertyId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(ct);
    }

    public Task<Property?> GetPropertyAsync(
        int propertyId,
        CancellationToken ct = default)
    {
        return _context.Properties
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ID == propertyId, ct);
    }

    public Task<Room?> GetRoomAsync(
        int propertyId,
        int roomId,
        bool tracking,
        CancellationToken ct = default)
    {
        IQueryable<Room> query = _context.Rooms
            .Include(x => x.Property)
            .Where(x => x.Id == roomId && x.property_id == propertyId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(ct);
    }

    public Task<User?> GetUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        return _context.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
    }

    public Task AddAsync(
        MaintenanceTicket ticket,
        CancellationToken ct = default)
    {
        return _context.MaintenanceTickets.AddAsync(ticket, ct).AsTask();
    }

    public Task AddAuditAsync(
        AuditLog audit,
        CancellationToken ct = default)
    {
        return _context.AuditLogs.AddAsync(audit, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
