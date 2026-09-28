using HotelHup.APPLICATION.DTO.housekeeping;
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

    public sealed class HousekeepingRepository : IHousekeepingRepository
    {
        private readonly hotelhupContext _context;
        public HousekeepingRepository(hotelhupContext context) => _context = context;

        public async Task<(IReadOnlyList<HousekeepingTask> Items, int TotalCount)> GetTasksAsync(HousekeepingTaskListRequest request, CancellationToken ct = default)
        {
            var query = _context.HousekeepingTasks.AsNoTracking()
                .Include(x => x.Room).Include(x => x.Assignee).AsQueryable();
            if (request.PropertyId.HasValue) query = query.Where(x => x.Room.property_id == request.PropertyId.Value);
            if (request.RoomId.HasValue) query = query.Where(x => x.RoomId == request.RoomId.Value);
            if (!string.IsNullOrWhiteSpace(request.AssigneeId)) query = query.Where(x => x.AssigneeId == request.AssigneeId);
            if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
            var total = await query.CountAsync(ct);
            var items = await query.OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
            return (items, total);
        }

        public Task<HousekeepingTask?> GetByIdAsync(int taskId, bool tracking, CancellationToken ct = default)
        {
            IQueryable<HousekeepingTask> query = _context.HousekeepingTasks.Include(x => x.Room).Include(x => x.Assignee).Where(x => x.id == taskId);
            return (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
        }

        public Task<HousekeepingTask?> GetForUpdateAsync(int taskId, CancellationToken ct = default) => GetByIdAsync(taskId, true, ct);

        public async Task<HousekeepingTask?> GetActiveForRoomAsync(int roomId, CancellationToken ct = default) =>
       await     _context.HousekeepingTasks.Include(x => x.Room).Include(x => x.Assignee)
                .Where(x => x.RoomId == roomId && x.Status == CORE.Enums.HousekeepingTaskStatus.Cleaning)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);

        public Task<Room?> GetRoomForUpdateAsync(int roomId, CancellationToken ct = default) =>
            _context.Rooms.Include(x => x.Property).ThenInclude(x => x.Settings)
                .SingleOrDefaultAsync(x => x.Id == roomId, ct);

        public async Task<bool> HasOpenTaskAsync(int roomId, int? excludingTaskId = null, CancellationToken ct = default) =>
         await  
            _context.HousekeepingTasks.AnyAsync
            (x => x.RoomId == roomId
            && x.Status != CORE.Enums.HousekeepingTaskStatus.Inspected
            && (!excludingTaskId.HasValue || x.id != excludingTaskId.Value), ct);

        public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default) =>
       await     _context.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);

        public async Task AddAsync(HousekeepingTask task, CancellationToken ct = default) 
            =>await _context.HousekeepingTasks.AddAsync(task, ct).AsTask();
        public async Task AddAuditAsync(AuditLog audit, CancellationToken ct = default) =>await _context.AuditLogs.AddAsync(audit, ct).AsTask();
        public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
        {
            if (_context.Database.CurrentTransaction is not null) return await operation();
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
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
}
