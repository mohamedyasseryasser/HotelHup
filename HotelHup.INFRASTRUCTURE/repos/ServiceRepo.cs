using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.CORE.Entities;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.INFRASTRUCTURE.repos;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly hotelhupContext _context;

    public ServiceRepository(hotelhupContext context)
    {
        _context = context;
    }

    public Task<Property?> GetPropertyAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        return _context.Properties
            .AsNoTracking()
            .SingleOrDefaultAsync(
                property => property.ID == propertyId,
                cancellationToken);
    }

    public async Task<Service?> GetByIdAsync(
        int propertyId,
        int serviceId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Service> query = _context.Services
            .Where(service =>
                service.id == serviceId &&
                service.propertyid == propertyId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Service> Items, int TotalCount)> GetListAsync(
        int propertyId,
        string? search,
        bool includeInactive,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Services
            .AsNoTracking()
            .Where(service => service.propertyid == propertyId);

        if (!includeInactive)
        {
            query = query.Where(service => service.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            query = query.Where(service =>
                service.Name.ToLower().Contains(value) ||
                (service.RevenueCategory != null &&
                 service.RevenueCategory.ToLower().Contains(value)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(service => service.Name)
            .ThenBy(service => service.id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(
        int propertyId,
        string normalizedName,
        int? excludingServiceId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.Services.AnyAsync(
            service =>
                service.propertyid == propertyId &&
                service.Name.ToLower() == normalizedName &&
                (!excludingServiceId.HasValue ||
                 service.id != excludingServiceId.Value),
            cancellationToken);
    }

    public async Task PersistAsync(
        Service service,
        AuditLog audit,
        CancellationToken cancellationToken = default)
    {
        if (service.id == 0)
        {
          await  _context.Services.AddAsync(service);
        }

       await _context.AuditLogs.AddAsync(audit);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
