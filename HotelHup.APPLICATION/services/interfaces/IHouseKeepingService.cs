using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.housekeeping;
using HotelHup.CORE.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.interfaces
{

    public interface IHousekeepingService
    {
        Task<ResponseStatus<PagedResponse<HousekeepingTaskResponseDto>>> GetTasksAsync(User actor, HousekeepingTaskListRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> GetTaskByIdAsync(User actor, int taskId, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> CreateTaskAsync(User actor, CreateHousekeepingTaskRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> AssignTaskAsync(User actor, int taskId, AssignHousekeepingTaskRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> StartCleaningAsync(User actor, int taskId, HousekeepingActionRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> CompleteCleaningAsync(User actor, int taskId, HousekeepingActionRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> CompleteCleaningByRoomAsync(User actor, int roomId, HousekeepingActionRequest request, CancellationToken ct = default);
        Task<ResponseStatus<HousekeepingTaskResponseDto>> InspectRoomAsync(User actor, int taskId, HousekeepingActionRequest request, CancellationToken ct = default);
    }

}
