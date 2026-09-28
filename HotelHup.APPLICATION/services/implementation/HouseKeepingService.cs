using System.Text.Json;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.housekeeping;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class HousekeepingService : IHousekeepingService
{
    private readonly IHousekeepingRepository _repository;
    private readonly IUserRepository _users;

    public HousekeepingService(
        IHousekeepingRepository repository,
        IUserRepository users)
    {
        _repository = repository;
        _users = users;
    }

    public async Task<ResponseStatus<PagedResponse<HousekeepingTaskResponseDto>>>
        GetTasksAsync(
            User actor,
            HousekeepingTaskListRequest request,
            CancellationToken ct = default)
    {
        var auth = await AuthorizeActorAsync(
            actor,
            Permissions.Housekeeping.Read,
            ct);

        if (!auth.Success)
        {
            return Fail<PagedResponse<HousekeepingTaskResponseDto>>(
                auth.Message,
                auth.Code,
                auth.StatusCode);
        }

        request ??= new HousekeepingTaskListRequest();

        var effective = request;

        if (!IsGlobalAdmin(actor))
        {
            if (!actor.PropertyId.HasValue)
            {
                return Fail<PagedResponse<HousekeepingTaskResponseDto>>(
                    "Property access is required.",
                    "PROPERTY_ACCESS_DENIED",
                    403);
            }

            // Business Rule:
            // A non-global-admin user can only access tasks that belong to their own property.
            if (request.PropertyId.HasValue &&
                request.PropertyId.Value != actor.PropertyId.Value)
            {
                return Fail<PagedResponse<HousekeepingTaskResponseDto>>(
                    "You are not authorized to access this property.",
                    "PROPERTY_ACCESS_DENIED",
                    403);
            }

            if (await IsHousekeeper(actor, ct))
            {
                // Business Rule:
                // A housekeeper can only see housekeeping tasks assigned to themselves.
                effective = new HousekeepingTaskListRequest
                {
                    PropertyId = actor.PropertyId,
                    AssigneeId = actor.Id,
                    RoomId = request.RoomId,
                    Status = request.Status,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            else
            {
                effective = new HousekeepingTaskListRequest
                {
                    PropertyId = actor.PropertyId,
                    RoomId = request.RoomId,
                    AssigneeId = request.AssigneeId,
                    Status = request.Status,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
        }

        var result = await _repository.GetTasksAsync(effective, ct);

        return Ok(
            new PagedResponse<HousekeepingTaskResponseDto>
            {
                Items = result.Items.Select(Map).ToList(),
                PageNumber = effective.PageNumber,
                PageSize = effective.PageSize,
                TotalCount = result.TotalCount
            });
    }

    public async Task<ResponseStatus<HousekeepingTaskResponseDto>>
        GetTaskByIdAsync(
            User actor,
            int taskId,
            CancellationToken ct = default)
    {
        var task = await _repository.GetByIdAsync(
            taskId,
            false,
            ct);

        if (task is null)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "Housekeeping task was not found.",
                "HOUSEKEEPING_TASK_NOT_FOUND",
                404);
        }

        var auth = await AuthorizeTaskAsync(
            actor,
            task,
            Permissions.Housekeeping.Read,
            ct);

        return auth.Success
            ? Ok(Map(task))
            : Fail<HousekeepingTaskResponseDto>(
                auth.Message,
                auth.Code,
                auth.StatusCode);
    }

    public async Task<ResponseStatus<HousekeepingTaskResponseDto>>
        CreateTaskAsync(
            User actor,
            CreateHousekeepingTaskRequest request,
            CancellationToken ct = default)
    {
        if (request is null || request.RoomId <= 0)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "RoomId is required.",
                "ROOM_REQUIRED",
                422);
        }

        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var room = await _repository.GetRoomForUpdateAsync(
                    request.RoomId,
                    ct);

                if (room is null)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Room was not found.",
                        "ROOM_NOT_FOUND",
                        404);
                }

                var auth = await AuthorizePropertyAsync(
                    actor,
                    room.property_id,
                    Permissions.Housekeeping.Assign,
                    ct);

                if (!auth.Success)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        auth.Message,
                        auth.Code,
                        auth.StatusCode);
                }

                // Business Rule:
                // Only Admin, Manager, or Receptionist users can create housekeeping tasks.
                if (!await CanCreateTaskAsync(actor, ct))
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Only Admin, Manager, or Receptionist users can create housekeeping tasks.",
                        "UNAUTHORIZED_HOUSEKEEPING_OPERATION",
                        403);
                }

                // Business Rule:
                // A housekeeping task can only be created when the room is Dirty.
                if (room.Status is not RoomStatus.Dirty)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "A housekeeping task can only be created for a dirty room.",
                        "INVALID_ROOM_STATUS",
                        409);
                }

                // Business Rule:
                // A room cannot have more than one open housekeeping task at the same time.
                // Pending and Cleaning tasks are considered open.
                if (await _repository.HasOpenTaskAsync(room.Id, null, ct))
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "The room already has an open housekeeping task.",
                        "OPEN_TASK_EXISTS",
                        409);
                }

                var now = DateTimeOffset.UtcNow;

                var task = new HousekeepingTask
                {
                    RoomId = room.Id,
                    Status = HousekeepingTaskStatus.Pending,
                    DueAt = request.DueAt,
                    Notes = request.Notes?.Trim(),
                    CreatedAt = now,
                    CreatedBy = actor.Id,
                    Room = room
                };

                await _repository.AddAsync(task, ct);

                await _repository.SaveChangesAsync(ct);

                await _repository.AddAuditAsync(
                    Audit(
                        actor,
                        room,
                        task,
                        "TASK_CREATED",
                        null,
                        task),
                    ct);

                await _repository.SaveChangesAsync(ct);

                return Ok(
                    Map(task),
                    "Housekeeping task created successfully.",
                    201);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "The room was modified by another request.",
                "CONCURRENCY_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<HousekeepingTaskResponseDto>>
        AssignTaskAsync(
            User actor,
            int taskId,
            AssignHousekeepingTaskRequest request,
            CancellationToken ct = default)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.AssigneeId))
        {
            return Fail<HousekeepingTaskResponseDto>(
                "AssigneeId is required.",
                "ASSIGNEE_REQUIRED",
                422);
        }

        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var task = await _repository.GetForUpdateAsync(
                    taskId,
                    ct);

                if (task is null)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Housekeeping task was not found.",
                        "HOUSEKEEPING_TASK_NOT_FOUND",
                        404);
                }

                var auth = await AuthorizeTaskAsync(
                    actor,
                    task,
                    Permissions.Housekeeping.Assign,
                    ct);

                if (!auth.Success)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        auth.Message,
                        auth.Code,
                        auth.StatusCode);
                }

                // Business Rule:
                // Only Pending housekeeping tasks can be assigned.
                if (task.Status != HousekeepingTaskStatus.Pending)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Only pending tasks can be assigned.",
                        "INVALID_TASK_STATUS",
                        409);
                }

                var assignee = await _repository.GetUserAsync(
                    request.AssigneeId.Trim(),
                    ct);

                if (assignee is null)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Assignee was not found.",
                        "ASSIGNEE_NOT_FOUND",
                        404);
                }

                // Business Rule:
                // Inactive users cannot be assigned housekeeping tasks.
                if (!assignee.IsActive)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Inactive users cannot be assigned tasks.",
                        "ASSIGNEE_INACTIVE",
                        409);
                }

                var roles = await _users.GetRolesAsync(
                    assignee.Id,
                    ct);

                // Business Rule:
                // The assignee must have an active Housekeeper role.
                if (!roles.Any(x =>
                    x.IsActive &&
                    string.Equals(
                        x.Name,
                        nameof(UserRole.Housekeeper),
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "must be housekeeper.",
                        "MUST BE HOUSEKEEPER",
                        409);
                }

                // Business Rule:
                // A housekeeper must belong to the same property as the room/task.
                // A global admin is allowed to assign across properties.
                if (assignee.PropertyId != task.Room.property_id &&
                    !await IsGlobalAdminAsync(assignee.Id, ct))
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Assignee belongs to another property.",
                        "PROPERTY_ACCESS_DENIED",
                        403);
                }

                var old = task.AssigneeId;

                task.AssigneeId = assignee.Id;
                task.Assignee = assignee;
                task.UpdatedAt = DateTimeOffset.UtcNow;
                task.UpdatedBy = actor.Id;

                await _repository.AddAuditAsync(
                    Audit(
                        actor,
                        task.Room,
                        task,
                        "TASK_ASSIGNED",
                        new
                        {
                            AssigneeId = old
                        },
                        new
                        {
                            task.AssigneeId
                        }),
                    ct);

                await _repository.SaveChangesAsync(ct);

                return Ok(
                    Map(task),
                    "Housekeeping task assigned successfully.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "The task was modified by another request.",
                "CONCURRENCY_CONFLICT",
                409);
        }
    }

    public Task<ResponseStatus<HousekeepingTaskResponseDto>>
        StartCleaningAsync(
            User actor,
            int taskId,
            HousekeepingActionRequest request,
            CancellationToken ct = default) =>
        TransitionAsync(
            actor,
            taskId,
            request,
            "start",
            ct);

    public Task<ResponseStatus<HousekeepingTaskResponseDto>>
        CompleteCleaningAsync(
            User actor,
            int taskId,
            HousekeepingActionRequest request,
            CancellationToken ct = default) =>
        TransitionAsync(
            actor,
            taskId,
            request,
            "complete",
            ct);

    public async Task<ResponseStatus<HousekeepingTaskResponseDto>>
        CompleteCleaningByRoomAsync(
            User actor,
            int roomId,
            HousekeepingActionRequest request,
            CancellationToken ct = default)
    {
        var task = await _repository.GetActiveForRoomAsync(
            roomId,
            ct);

        // Business Rule:
        // Cleaning can only be completed when the room has an active housekeeping task.
        if (task is null)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "No active cleaning task was found for this room.",
                "HOUSEKEEPING_TASK_NOT_FOUND",
                404);
        }

        return await CompleteCleaningAsync(
            actor,
            task.id,
            request,
            ct);
    }

    public Task<ResponseStatus<HousekeepingTaskResponseDto>>
        InspectRoomAsync(
            User actor,
            int taskId,
            HousekeepingActionRequest request,
            CancellationToken ct = default) =>
        TransitionAsync(
            actor,
            taskId,
            request,
            "inspect",
            ct);

    private async Task<ResponseStatus<HousekeepingTaskResponseDto>>
        TransitionAsync(
            User actor,
            int taskId,
            HousekeepingActionRequest request,
            string operation,
            CancellationToken ct)
    {
        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var task = await _repository.GetForUpdateAsync(
                    taskId,
                    ct);

                if (task is null)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Housekeeping task was not found.",
                        "HOUSEKEEPING_TASK_NOT_FOUND",
                        404);
                }

                var permission =
                    operation == "start"
                        ? Permissions.Housekeeping.Start
                        : Permissions.Housekeeping.Complete;

                var auth = await AuthorizeTaskAsync(
                    actor,
                    task,
                    permission,
                    ct);

                if (!auth.Success)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        auth.Message,
                        auth.Code,
                        auth.StatusCode);
                }

                var room = await _repository.GetRoomForUpdateAsync(
                    task.RoomId,
                    ct);

                if (room is null)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Room was not found.",
                        "ROOM_NOT_FOUND",
                        404);
                }

                // Business Rule:
                // OutOfOrder and OutOfService rooms are controlled by maintenance
                // and cannot go through the normal housekeeping workflow.
                if (room.Status is RoomStatus.OutOfOrder or RoomStatus.OutOfService)
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "Out-of-order rooms are controlled by maintenance.",
                        "ROOM_OUT_OF_ORDER",
                        409);
                }

                // Business Rule:
                // A housekeeping task must be assigned before a housekeeper can
                // start or complete cleaning.
                if (task.AssigneeId is null && operation != "inspect")
                {
                    return Fail<HousekeepingTaskResponseDto>(
                        "The task must be assigned first.",
                        "TASK_NOT_ASSIGNED",
                        409);
                }

                var oldTask = new
                {
                    task.Status,
                    task.StartedAt,
                    task.CompletedAt
                };

                var oldRoom = room.Status;

                var now = DateTimeOffset.UtcNow;

                if (operation == "start")
                {
                    // Business Rule:
                    // Cleaning can only start from the Pending state.
                    if (task.Status != HousekeepingTaskStatus.Pending)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "The task has already started or completed.",
                            "TASK_ALREADY_STARTED",
                            409);
                    }

                    // Business Rule:
                    // A housekeeping task can only start cleaning when the room is Dirty.
                    if (room.Status != RoomStatus.Dirty)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "The room is not ready for cleaning.",
                            "ROOM_NOT_READY_FOR_CLEANING",
                            409);
                    }

                    task.Status = HousekeepingTaskStatus.Cleaning;
                    task.StartedAt = now;

                    // Business Rule:
                    // When cleaning starts, the room status must change to Cleaning.
                    room.Status = RoomStatus.Cleaning;
                }
                else if (operation == "complete")
                {
                    // Business Rule:
                    // A task that is already Completed cannot be completed again.
                    if (task.Status == HousekeepingTaskStatus.Completed)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "The task is already completed.",
                            "TASK_ALREADY_COMPLETED",
                            409);
                    }

                    // Business Rule:
                    // Cleaning must be started before it can be completed.
                    if (task.Status != HousekeepingTaskStatus.Cleaning ||
                        task.StartedAt is null)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "Cleaning must be started before it can be completed.",
                            "INVALID_TASK_STATUS",
                            409);
                    }

                    // Business Rule:
                    // The room must still be in Cleaning state when the task is completed.
                    if (room.Status != RoomStatus.Cleaning)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "The room is not currently being cleaned.",
                            "INVALID_ROOM_STATUS",
                            409);
                    }

                    task.Status = HousekeepingTaskStatus.Completed;
                    task.CompletedAt = now;

                    // Business Rule:
                    // If inspection is required, the room remains unavailable until inspection.
                    // Otherwise, the room becomes Available immediately after cleaning.
                    room.Status =
                        room.Property?.Settings?.RequireInspectionBeforeAvailable == true
                            ? RoomStatus.Cleaning
                            : RoomStatus.Available;
                }
                else
                {
                    var requiresInspection =
                        room.Property?.Settings?.RequireInspectionBeforeAvailable == true;

                    // Business Rule:
                    // Inspection can only be performed when the property requires inspection.
                    if (!requiresInspection)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "Inspection is not required for this property.",
                            "INSPECTION_NOT_REQUIRED",
                            409);
                    }

                    // Business Rule:
                    // Inspection is only allowed after cleaning has been completed
                    // and the room is still waiting for inspection.
                    if (task.Status != HousekeepingTaskStatus.Completed ||
                        room.Status != RoomStatus.Cleaning)
                    {
                        return Fail<HousekeepingTaskResponseDto>(
                            "The task is not ready for inspection.",
                            "INSPECTION_REQUIRED",
                            409);
                    }

                    task.Status = HousekeepingTaskStatus.Inspected;

                    // Business Rule:
                    // Once the completed cleaning task passes inspection,
                    // the room becomes Available.
                    room.Status = RoomStatus.Available;
                }

                task.Notes =
                    string.IsNullOrWhiteSpace(request?.Notes)
                        ? task.Notes
                        : request.Notes.Trim();

                task.UpdatedAt = now;
                task.UpdatedBy = actor.Id;

                await _repository.AddAuditAsync(
                    Audit(
                        actor,
                        room,
                        task,
                        operation == "start"
                            ? "CLEANING_STARTED"
                            : operation == "complete"
                                ? "CLEANING_COMPLETED"
                                : "ROOM_INSPECTED",
                        oldTask,
                        new
                        {
                            task.Status,
                            task.StartedAt,
                            task.CompletedAt
                        }),
                    ct);

                await _repository.AddAuditAsync(
                    Audit(
                        actor,
                        room,
                        task,
                        "ROOM_STATUS_CHANGED",
                        oldRoom,
                        room.Status),
                    ct);

                await _repository.SaveChangesAsync(ct);

                return Ok(
                    Map(task),
                    operation == "start"
                        ? "Cleaning started."
                        : operation == "complete"
                            ? "Cleaning completed."
                            : "Room inspected.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<HousekeepingTaskResponseDto>(
                "The room or task was modified by another request.",
                "CONCURRENCY_CONFLICT",
                409);
        }
    }

    private async Task<ResponseStatus<bool>>
        AuthorizeTaskAsync(
            User actor,
            HousekeepingTask task,
            string permission,
            CancellationToken ct)
    {
        var auth = await AuthorizePropertyAsync(
            actor,
            task.Room.property_id,
            permission,
            ct);

        if (!auth.Success)
        {
            return auth;
        }

        // Business Rule:
        // A housekeeper can only access housekeeping tasks assigned to themselves.
        // Global admins are exempt from this restriction.
        if (await IsHousekeeper(actor, ct) &&
            task.AssigneeId != actor.Id &&
            !IsGlobalAdmin(actor))
        {
            return Fail<bool>(
                "Housekeepers can only access assigned tasks.",
                "UNAUTHORIZED_HOUSEKEEPING_OPERATION",
                403);
        }

        return Ok(true);
    }

    private async Task<ResponseStatus<bool>>
        AuthorizePropertyAsync(
            User actor,
            int propertyId,
            string permission,
            CancellationToken ct)
    {
        var auth = await AuthorizeActorAsync(
            actor,
            permission,
            ct);

        if (!auth.Success)
        {
            return auth;
        }

        // Business Rule:
        // Non-global-admin users can only operate on resources belonging to their property.
        if (!IsGlobalAdmin(actor) &&
            actor.PropertyId != propertyId)
        {
            return Fail<bool>(
                "You are not authorized to access this property.",
                "PROPERTY_ACCESS_DENIED",
                403);
        }

        return Ok(true);
    }

    private async Task<ResponseStatus<bool>>
        AuthorizeActorAsync(
            User actor,
            string permission,
            CancellationToken ct)
    {
        // Business Rule:
        // Inactive users cannot perform housekeeping operations.
        if (actor is null || !actor.IsActive)
        {
            return Fail<bool>(
                "User is not authenticated.",
                "UNAUTHENTICATED",
                401);
        }

        // Business Rule:
        // Global admins bypass property-level permission checks.
        if (IsGlobalAdmin(actor))
        {
            return Ok(true);
        }

        var permissions = await _users.GetPermissionNamesAsync(
            actor.Id,
            ct);

        // Business Rule:
        // The actor must have the required permission to perform the operation.
        return permissions.Contains(permission)
            ? Ok(true)
            : Fail<bool>(
                "You are not allowed to perform this housekeeping operation.",
                "UNAUTHORIZED_HOUSEKEEPING_OPERATION",
                403);
    }

    private bool IsGlobalAdmin(User actor) =>
        actor.PropertyId is null;

    private async Task<bool>
        IsGlobalAdminAsync(
            string userId,
            CancellationToken ct) =>
        (await _users.GetByIdAsync(userId, ct))?.PropertyId is null;

    private async Task<bool>
        IsHousekeeper(
            User actor,
            CancellationToken ct) =>
        (await _users.GetRolesAsync(
            actor.Id,
            ct))
        .Any(x =>
            x.IsActive &&
            string.Equals(
                x.Name,
                nameof(UserRole.Housekeeper),
                StringComparison.OrdinalIgnoreCase));

    private async Task<bool>
        CanCreateTaskAsync(
            User actor,
            CancellationToken ct)
    {
        var roles = await _users.GetRolesAsync(
            actor.Id,
            ct);

        // Business Rule:
        // Only users with an active Admin, Manager, or Receptionist role
        // are allowed to create housekeeping tasks.
        return roles.Any(x =>
            x.IsActive &&
            (
                string.Equals(
                    x.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase)

                || string.Equals(
                    x.Name,
                    nameof(UserRole.Manager),
                    StringComparison.OrdinalIgnoreCase)

                || string.Equals(
                    x.Name,
                    nameof(UserRole.Receptionist),
                    StringComparison.OrdinalIgnoreCase)
            ));
    }

    private static AuditLog Audit(
        User actor,
        Room room,
        HousekeepingTask task,
        string action,
        object? oldValues,
        object? newValues) =>
        new()
        {
            UserId = actor.Id,
            PropertyId = room.property_id,
            EntityName =
                action == "ROOM_STATUS_CHANGED"
                    ? nameof(Room)
                    : nameof(HousekeepingTask),
            EntityId =
                action == "ROOM_STATUS_CHANGED"
                    ? room.Id.ToString()
                    : task.id.ToString(),
            Action = action,
            OldValues =
                oldValues is null
                    ? null
                    : JsonSerializer.Serialize(oldValues),
            NewValues =
                newValues is null
                    ? null
                    : JsonSerializer.Serialize(newValues),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };

    private static HousekeepingTaskResponseDto Map(
        HousekeepingTask x) =>
        new()
        {
            Id = x.id,
            RoomId = x.RoomId,
            PropertyId = x.Room.property_id,
            RoomNumber = x.Room.RoomNumber,
            Status = x.Status,
            AssigneeId = x.AssigneeId,
            AssigneeName = x.Assignee?.FullName,
            DueAt = x.DueAt,
            StartedAt = x.StartedAt,
            CompletedAt = x.CompletedAt,
            Notes = x.Notes,
            RoomStatus = x.Room.Status
        };

    private static ResponseStatus<T>
        Ok<T>(
            T value,
            string message = "",
            int status = 200) =>
        new(value, message, status);

    private static ResponseStatus<T>
        Fail<T>(
            string message,
            string? code,
            int status) =>
        new(
            message: message,
            code: code,
            statusCode: status);
}