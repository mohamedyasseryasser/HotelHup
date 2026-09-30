using System.ComponentModel.DataAnnotations;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.DTO.maintenance;

public sealed class MaintenanceListRequest
{
    public int? PropertyId { get; init; }
    public int? RoomId { get; init; }
    public string? AssigneeId { get; init; }
    public MaintenanceStatus? Status { get; init; }
    public MaintenancePriority? Priority { get; init; }
    [Range(1, 100)] public int PageSize { get; init; } = 20;
    [Range(1, int.MaxValue)] public int PageNumber { get; init; } = 1;
}

public sealed class CreateMaintenanceRequest
{
    [Range(1, int.MaxValue)] public int PropertyId { get; init; }
    [Range(1, int.MaxValue)] public int RoomId { get; init; }
    [Required, MaxLength(1000)] public string Issue { get; init; } = string.Empty;
    public MaintenancePriority Priority { get; init; } = MaintenancePriority.Medium;
    public string? AssigneeId { get; init; }
}

public sealed class UpdateMaintenanceRequest
{
    [Required, MaxLength(1000)] public string Issue { get; init; } = string.Empty;
    public MaintenancePriority Priority { get; init; }
}

public sealed class AssignMaintenanceRequest
{
    [Required] public string AssigneeId { get; init; } = string.Empty;
}

public sealed class MaintenanceActionRequest
{
    [MaxLength(1000)] public string? Reason { get; init; }
}

public sealed class MaintenanceResponseDto
{
    public int Id { get; init; }
    public int PropertyId { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string Issue { get; init; } = string.Empty;
    public MaintenancePriority Priority { get; init; }
    public MaintenanceStatus Status { get; init; }
    public string? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
