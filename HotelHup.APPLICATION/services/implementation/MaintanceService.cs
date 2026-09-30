using System.Text.Json;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.maintenance;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _repository;
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MaintenanceService(
        IMaintenanceRepository repository,
        IUserRepository users,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _users = users;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResponseStatus<PagedResponse<MaintenanceResponseDto>>> GetListAsync(
        User actor,
        MaintenanceListRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            request = new MaintenanceListRequest();
        }

        var permission = await AuthorizePermissionAsync(
            actor,
            request.PropertyId,
            Permissions.Maintenance.Read,
            ct);

        if (!permission.Success)
        {
            return Fail<PagedResponse<MaintenanceResponseDto>>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        var globalAdmin = await IsGlobalAdminAsync(actor, ct);
        var effectivePropertyId = globalAdmin
            ? request.PropertyId
            : actor.PropertyId;

        var effectiveRequest = new MaintenanceListRequest
        {
            PropertyId = effectivePropertyId,
            RoomId = request.RoomId,
            AssigneeId = request.AssigneeId,
            Status = request.Status,
            Priority = request.Priority,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var result = await _repository.GetPagedAsync(effectiveRequest, ct);

        return Ok(new PagedResponse<MaintenanceResponseDto>
        {
            Items = result.Items.Select(Map).ToList(),
            PageNumber = effectiveRequest.PageNumber,
            PageSize = effectiveRequest.PageSize,
            TotalCount = result.TotalCount
        });
    }

    public async Task<ResponseStatus<MaintenanceResponseDto>> GetAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        CancellationToken ct = default)
    {
        var permission = await AuthorizePermissionAsync(
            actor,
            propertyId,
            Permissions.Maintenance.Read,
            ct);

        if (!permission.Success)
        {
            return Fail<MaintenanceResponseDto>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        var ticket = await _repository.GetByIdAndPropertyAsync(
            maintenanceId,
            propertyId,
            false,
            ct);

        return ticket is null
            ? Fail<MaintenanceResponseDto>(
                "Maintenance ticket was not found.",
                "MAINTENANCE_NOT_FOUND",
                404)
            : Ok(Map(ticket));
    }

    public async Task<ResponseStatus<MaintenanceResponseDto>> CreateAsync(
        User actor,
        CreateMaintenanceRequest request,
        CancellationToken ct = default)
    {
        if (request is null || request.PropertyId <= 0 || request.RoomId <= 0)
        {
            return Fail<MaintenanceResponseDto>(
                "PropertyId and RoomId are required.",
                "MAINTENANCE_LOCATION_REQUIRED",
                422);
        }

        var issue = request.Issue?.Trim();
        if (string.IsNullOrWhiteSpace(issue))
        {
            return Fail<MaintenanceResponseDto>(
                "Issue is required.",
                "ISSUE_REQUIRED",
                422);
        }

        if (!Enum.IsDefined(request.Priority))
        {
            return Fail<MaintenanceResponseDto>(
                "Maintenance priority is invalid.",
                "INVALID_MAINTENANCE_PRIORITY",
                422);
        }

        var permission = await AuthorizePermissionAsync(
            actor,
            request.PropertyId,
            Permissions.Maintenance.Create,
            ct);

        if (!permission.Success)
        {
            return Fail<MaintenanceResponseDto>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        var property = await _repository.GetPropertyAsync(request.PropertyId, ct);
        if (property is null)
        {
            return Fail<MaintenanceResponseDto>(
                "Property was not found.",
                "PROPERTY_NOT_FOUND",
                404);
        }

        if (property.Status != PropertyStatus.Active)
        {
            return Fail<MaintenanceResponseDto>(
                "Maintenance cannot be created for an inactive property.",
                "PROPERTY_INACTIVE",
                409);
        }

        // Business Rule:
        // The room is loaded with the requested PropertyId in the database query,
        // so a room from another property cannot be attached to this ticket.
        var room = await _repository.GetRoomAsync(
            request.PropertyId,
            request.RoomId,
            false,
            ct);

        if (room is null)
        {
            return Fail<MaintenanceResponseDto>(
                "Room was not found for this property.",
                "ROOM_PROPERTY_MISMATCH",
                404);
        }

        var ticket = new MaintenanceTicket
        {
            RoomId = room.Id,
            Room = room,
            Issue = issue,
            Priority = request.Priority,
            Status = MaintenanceStatus.Open,
            AssigneeId = null,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };

        if (!string.IsNullOrWhiteSpace(request.AssigneeId))
        {
            var assignResult = await ValidateAssigneeAsync(
                actor,
                request.PropertyId,
                request.AssigneeId.Trim(),
                ct);

            if (!assignResult.Success)
            {
                return Fail<MaintenanceResponseDto>(
                    assignResult.Message,
                    assignResult.Code ?? "ASSIGNEE_INVALID",
                    assignResult.StatusCode);
            }

            ticket.AssigneeId = request.AssigneeId.Trim();
        }

        try
        {
            return await _repository.ExecuteSerializableAsync(async () =>
            {
                await _repository.AddAsync(ticket, ct);
                await _repository.SaveChangesAsync(ct);
                await _repository.AddAuditAsync(
                    BuildAudit(
                        actor,
                        request.PropertyId,
                        ticket.id.ToString(),
                        "MAINTENANCE_CREATED",
                        null,
                        new
                        {
                            ticket.id,
                            ticket.RoomId,
                            ticket.Issue,
                            ticket.Priority,
                            ticket.Status,
                            ticket.AssigneeId
                        }),
                    ct);
                await _repository.SaveChangesAsync(ct);

                var created = await _repository.GetByIdAndPropertyAsync(
                    ticket.id,
                    request.PropertyId,
                    false,
                    ct);

                return Ok(
                    Map(created ?? ticket),
                    "Maintenance ticket created successfully.",
                    201);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<MaintenanceResponseDto>(
                "The maintenance ticket could not be created because related data changed.",
                "MAINTENANCE_CONCURRENCY_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<MaintenanceResponseDto>> UpdateAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        UpdateMaintenanceRequest request,
        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Issue))
        {
            return Fail<MaintenanceResponseDto>(
                "Issue is required.",
                "ISSUE_REQUIRED",
                422);
        }

        if (!Enum.IsDefined(request.Priority))
        {
            return Fail<MaintenanceResponseDto>(
                "Maintenance priority is invalid.",
                "INVALID_MAINTENANCE_PRIORITY",
                422);
        }

        var permission = await AuthorizePermissionAsync(
            actor,
            propertyId,
            Permissions.Maintenance.Update,
            ct);

        if (!permission.Success)
        {
            return Fail<MaintenanceResponseDto>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        var ticket = await _repository.GetByIdAndPropertyAsync(
            maintenanceId,
            propertyId,
            true,
            ct);

        if (ticket is null)
        {
            return Fail<MaintenanceResponseDto>(
                "Maintenance ticket was not found.",
                "MAINTENANCE_NOT_FOUND",
                404);
        }

        if (ticket.Status == MaintenanceStatus.Closed)
        {
            return Fail<MaintenanceResponseDto>(
                "A closed maintenance ticket cannot be modified.",
                "MAINTENANCE_CLOSED",
                409);
        }

        var oldValues = new
        {
            ticket.Issue,
            ticket.Priority
        };

        ticket.Issue = request.Issue.Trim();
        ticket.Priority = request.Priority;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        ticket.UpdatedBy = actor.Id;

        try
        {
            return await _repository.ExecuteSerializableAsync(async () =>
            {
                await _repository.AddAuditAsync(
                    BuildAudit(
                        actor,
                        propertyId,
                        ticket.id.ToString(),
                        "MAINTENANCE_UPDATED",
                        oldValues,
                        new
                        {
                            ticket.Issue,
                            ticket.Priority
                        }),
                    ct);
                await _repository.SaveChangesAsync(ct);
                return Ok(Map(ticket), "Maintenance ticket updated successfully.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<MaintenanceResponseDto>(
                "The maintenance ticket was modified by another request.",
                "MAINTENANCE_CONCURRENCY_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<MaintenanceResponseDto>> AssignAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        AssignMaintenanceRequest request,
        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.AssigneeId))
        {
            return Fail<MaintenanceResponseDto>(
                "AssigneeId is required.",
                "ASSIGNEE_REQUIRED",
                422);
        }

        var permission = await AuthorizePermissionAsync(
            actor,
            propertyId,
            Permissions.Maintenance.Assign,
            ct);

        if (!permission.Success)
        {
            return Fail<MaintenanceResponseDto>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        return await _repository.ExecuteSerializableAsync(async () =>
        {
            var ticket = await _repository.GetByIdAndPropertyAsync(
                maintenanceId,
                propertyId,
                true,
                ct);

            if (ticket is null)
            {
                return Fail<MaintenanceResponseDto>(
                    "Maintenance ticket was not found.",
                    "MAINTENANCE_NOT_FOUND",
                    404);
            }

            // Business Rule:
            // Closed tickets are historical records and cannot be reassigned.
            if (ticket.Status == MaintenanceStatus.Closed)
            {
                return Fail<MaintenanceResponseDto>(
                    "A closed maintenance ticket cannot be reassigned.",
                    "MAINTENANCE_CLOSED",
                    409);
            }

            var assigneeId = request.AssigneeId.Trim();
            var assignee = await ValidateAssigneeAsync(
                actor,
                propertyId,
                assigneeId,
                ct);

            if (!assignee.Success)
            {
                return Fail<MaintenanceResponseDto>(
                    assignee.Message,
                    assignee.Code ?? "ASSIGNEE_INVALID",
                    assignee.StatusCode);
            }

            var oldAssigneeId = ticket.AssigneeId;
            ticket.AssigneeId = assigneeId;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;
            ticket.UpdatedBy = actor.Id;

            await _repository.AddAuditAsync(
                BuildAudit(
                    actor,
                    propertyId,
                    ticket.id.ToString(),
                    "MAINTENANCE_ASSIGNED",
                    new { AssigneeId = oldAssigneeId },
                    new { AssigneeId = ticket.AssigneeId }),
                ct);
            await _repository.SaveChangesAsync(ct);
            return Ok(Map(ticket), "Maintenance ticket assigned successfully.");
        }, ct);
    }

    public Task<ResponseStatus<MaintenanceResponseDto>> StartAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        MaintenanceActionRequest request,
        CancellationToken ct = default)
    {
        return TransitionAsync(
            actor,
            propertyId,
            maintenanceId,
            request,
            MaintenanceStatus.InProgress,
            Permissions.Maintenance.Update,
            ct);
    }

    public Task<ResponseStatus<MaintenanceResponseDto>> CompleteAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        MaintenanceActionRequest request,
        CancellationToken ct = default)
    {
        return TransitionAsync(
            actor,
            propertyId,
            maintenanceId,
            request,
            MaintenanceStatus.Closed,
            Permissions.Maintenance.Complete,
            ct);
    }

    private async Task<ResponseStatus<MaintenanceResponseDto>> TransitionAsync(
        User actor,
        int propertyId,
        int maintenanceId,
        MaintenanceActionRequest request,
        MaintenanceStatus targetStatus,
        string permissionName,
        CancellationToken ct)
    {
        var permission = await AuthorizePermissionAsync(
            actor,
            propertyId,
            permissionName,
            ct);

        if (!permission.Success)
        {
            return Fail<MaintenanceResponseDto>(
                permission.Message,
                permission.Code ?? "AUTHORIZATION_FAILED",
                permission.StatusCode);
        }

        try
        {
            return await _repository.ExecuteSerializableAsync(async () =>
            {
                var ticket = await _repository.GetByIdAndPropertyAsync(
                    maintenanceId,
                    propertyId,
                    true,
                    ct);

                if (ticket is null)
                {
                    return Fail<MaintenanceResponseDto>(
                        "Maintenance ticket was not found.",
                        "MAINTENANCE_NOT_FOUND",
                        404);
                }

                var oldStatus = ticket.Status;

                // Business Rule:
                // A maintenance ticket can start only from Open.
                if (targetStatus == MaintenanceStatus.InProgress &&
                    ticket.Status != MaintenanceStatus.Open)
                {
                    return Fail<MaintenanceResponseDto>(
                        "Only open maintenance tickets can be started.",
                        "INVALID_MAINTENANCE_STATUS",
                        409);
                }

                // Business Rule:
                // A maintenance ticket can be completed only after work is in progress.
                if (targetStatus == MaintenanceStatus.Closed &&
                    ticket.Status != MaintenanceStatus.InProgress)
                {
                    return Fail<MaintenanceResponseDto>(
                        "Only in-progress maintenance tickets can be completed.",
                        "INVALID_MAINTENANCE_STATUS",
                        409);
                }

                ticket.Status = targetStatus;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;
                ticket.UpdatedBy = actor.Id;

                if (targetStatus == MaintenanceStatus.Closed)
                {
                    ticket.ClosedAt = DateTimeOffset.UtcNow;
                }

                await _repository.AddAuditAsync(
                    BuildAudit(
                        actor,
                        propertyId,
                        ticket.id.ToString(),
                        targetStatus == MaintenanceStatus.Closed
                            ? "MAINTENANCE_COMPLETED"
                            : "MAINTENANCE_STARTED",
                        new
                        {
                            Status = oldStatus.ToString()
                        },
                        new
                        {
                            Status = ticket.Status.ToString(),
                            Reason = request?.Reason?.Trim()
                        }),
                    ct);
                await _repository.SaveChangesAsync(ct);
                return Ok(
                    Map(ticket),
                    targetStatus == MaintenanceStatus.Closed
                        ? "Maintenance ticket completed successfully."
                        : "Maintenance ticket started successfully.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<MaintenanceResponseDto>(
                "The maintenance ticket was modified by another request.",
                "MAINTENANCE_CONCURRENCY_CONFLICT",
                409);
        }
    }

    private async Task<ResponseStatus<bool>> AuthorizePermissionAsync(
        User actor,
        int? propertyId,
        string permissionName,
        CancellationToken ct)
    {
        if (actor is null || string.IsNullOrWhiteSpace(actor.Id))
        {
            return new ResponseStatus<bool>(
                "Authenticated user is required.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        var globalAdmin = await IsGlobalAdminAsync(actor, ct);
        if (!propertyId.HasValue && !globalAdmin)
        {
            return new ResponseStatus<bool>(
                "A valid property scope is required.",
                statusCode: 422,
                code: "PROPERTY_SCOPE_REQUIRED");
        }

        if (propertyId.HasValue)
        {
            var property = await _repository.GetPropertyAsync(propertyId.Value, ct);
            if (property is null)
            {
                return new ResponseStatus<bool>(
                    "Property was not found.",
                    statusCode: 404,
                    code: "PROPERTY_NOT_FOUND");
            }
        }

        if (!globalAdmin && actor.PropertyId != propertyId)
        {
            return new ResponseStatus<bool>(
                "You are not authorized to access this property.",
                statusCode: 403,
                code: "UNAUTHORIZED_PROPERTY_ACCESS");
        }

        var permissions = await _users.GetPermissionNamesAsync(actor.Id, ct);
        if (!globalAdmin &&
            !permissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase))
        {
            return new ResponseStatus<bool>(
                "You do not have permission to perform this maintenance operation.",
                statusCode: 403,
                code: "PERMISSION_DENIED");
        }

        return new ResponseStatus<bool>(true);
    }

    private async Task<ResponseStatus<bool>> ValidateAssigneeAsync(
        User actor,
        int propertyId,
        string assigneeId,
        CancellationToken ct)
    {
        var assignee = await _repository.GetUserAsync(assigneeId, ct);
        if (assignee is null)
        {
            return new ResponseStatus<bool>(
                "Assignee was not found.",
                statusCode: 404,
                code: "ASSIGNEE_NOT_FOUND");
        }

        if (!assignee.IsActive)
        {
            return new ResponseStatus<bool>(
                "Inactive users cannot be assigned maintenance tickets.",
                statusCode: 409,
                code: "ASSIGNEE_INACTIVE");
        }

        var roles = await _users.GetRolesAsync(assignee.Id, ct);
        var canMaintain = roles.Any(role =>
            role.IsActive &&
            (string.Equals(role.Name, nameof(UserRole.Maintenance), StringComparison.OrdinalIgnoreCase) ||
             string.Equals(role.Name, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)));

        if (!canMaintain)
        {
            return new ResponseStatus<bool>(
                "The assignee must have the Maintenance role.",
                statusCode: 409,
                code: "ASSIGNEE_ROLE_REQUIRED");
        }

        var assigneeIsGlobalAdmin = await IsGlobalAdminAsync(assignee, ct);
        if (assignee.PropertyId != propertyId && !assigneeIsGlobalAdmin)
        {
            return new ResponseStatus<bool>(
                "Assignee belongs to another property.",
                statusCode: 403,
                code: "ASSIGNEE_PROPERTY_MISMATCH");
        }

        return new ResponseStatus<bool>(true);
    }

    private async Task<bool> IsGlobalAdminAsync(
        User actor,
        CancellationToken ct)
    {
        if (actor.PropertyId.HasValue)
        {
            return false;
        }

        var roles = await _users.GetRolesAsync(actor.Id, ct);
        return roles.Any(role =>
            role.IsActive &&
            string.Equals(
                role.Name,
                nameof(UserRole.Admin),
                StringComparison.OrdinalIgnoreCase));
    }

    private MaintenanceResponseDto Map(MaintenanceTicket ticket)
    {
        return new MaintenanceResponseDto
        {
            Id = ticket.id,
            PropertyId = ticket.Room.property_id,
            RoomId = ticket.RoomId,
            RoomNumber = ticket.Room.RoomNumber,
            Issue = ticket.Issue,
            Priority = ticket.Priority,
            Status = ticket.Status,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            ClosedAt = ticket.ClosedAt,
            CreatedAt = ticket.CreatedAt,
            CreatedBy = ticket.CreatedBy,
            UpdatedAt = ticket.UpdatedAt,
            UpdatedBy = ticket.UpdatedBy
        };
    }

    private AuditLog BuildAudit(
        User actor,
        int propertyId,
        string entityId,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            UserId = actor.Id,
            PropertyId = propertyId,
            EntityName = nameof(MaintenanceTicket),
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CorrelationId = GetCorrelationId(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };
    }

    private string GetCorrelationId()
    {
        return _httpContextAccessor.HttpContext?.TraceIdentifier
            ?? Guid.NewGuid().ToString("N");
    }

    private static ResponseStatus<T> Ok<T>(
        T data,
        string message = "",
        int statusCode = 200)
    {
        return new ResponseStatus<T>(data, message, statusCode);
    }

    private static ResponseStatus<T> Fail<T>(
        string message,
        string code,
        int statusCode)
    {
        return new ResponseStatus<T>(message, statusCode: statusCode, code: code);
    }
}
