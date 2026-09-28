using HotelHup.CORE.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.housekeeping
{

    public sealed class HousekeepingTaskListRequest
    {
        public int? PropertyId { get; init; }
        public int? RoomId { get; init; }
        public string? AssigneeId { get; init; }
        public HousekeepingTaskStatus? Status { get; init; }
        [Range(1, 100)] public int PageSize { get; init; } = 20;
        [Range(1, int.MaxValue)] public int PageNumber { get; init; } = 1;
    }

    public sealed class CreateHousekeepingTaskRequest
    {
        [Range(1, int.MaxValue)] public int RoomId { get; init; }
        public DateTimeOffset? DueAt { get; init; }
        [MaxLength(1000)] public string? Notes { get; init; }
    }

    public sealed class AssignHousekeepingTaskRequest
    {
        [Required] public string AssigneeId { get; init; } = string.Empty;
    }

    public sealed class HousekeepingActionRequest
    {
        [MaxLength(1000)] public string? Notes { get; init; }
    }

    public sealed class HousekeepingTaskResponseDto
    {
        public int Id { get; init; }
        public int RoomId { get; init; }
        public int PropertyId { get; init; }
        public string RoomNumber { get; init; } = string.Empty;
        public HousekeepingTaskStatus Status { get; init; }
        public string? AssigneeId { get; init; }
        public string? AssigneeName { get; init; }
        public DateTimeOffset? DueAt { get; init; }
        public DateTimeOffset? StartedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public string? Notes { get; init; }
        public RoomStatus RoomStatus { get; init; }
    }

}
