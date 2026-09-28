using HotelHup.APPLICATION.DTO.housekeeping;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.interfacesrepo
{

    public interface IHousekeepingRepository
    {
        Task<(IReadOnlyList<HousekeepingTask> Items, int TotalCount)> GetTasksAsync(HousekeepingTaskListRequest request, CancellationToken ct = default);
        Task<HousekeepingTask?> GetByIdAsync(int taskId, bool tracking, CancellationToken ct = default);
        Task<HousekeepingTask?> GetForUpdateAsync(int taskId, CancellationToken ct = default);
        Task<HousekeepingTask?> GetActiveForRoomAsync(int roomId, CancellationToken ct = default);
        Task<Room?> GetRoomForUpdateAsync(int roomId, CancellationToken ct = default);
        Task<bool> HasOpenTaskAsync(int roomId, int? excludingTaskId = null, CancellationToken ct = default);
        Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
        Task AddAsync(HousekeepingTask task, CancellationToken ct = default);
        Task AddAuditAsync(AuditLog audit, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    }

}
