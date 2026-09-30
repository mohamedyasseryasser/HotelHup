using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.services;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class ServiceService : IServiceService
{
    private readonly IServiceRepository _repository;
    private readonly IUserRepository _users;

    public ServiceService(
        IServiceRepository repository,
        IUserRepository users)
    {
        _repository = repository;
        _users = users;
    }

    public async Task<ResponseStatus<PagedResponse<ServiceResponse>>> GetListAsync(
        User actor,
        ServiceListRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Fail<PagedResponse<ServiceResponse>>(
                "Request is required.",
                "VALIDATION_ERROR",
                400);
        }

        var propertyId = request.PropertyId ?? actor.PropertyId;
        if (!propertyId.HasValue)
        {
            return Fail<PagedResponse<ServiceResponse>>(
                "PropertyId is required.",
                "PROPERTY_REQUIRED",
                400);
        }

        var access = await AuthorizeAsync(
            actor,
            propertyId.Value,
            Permissions.Services.Read,
            cancellationToken);

        if (!access.Success)
        {
            return Fail<PagedResponse<ServiceResponse>>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var property = await _repository.GetPropertyAsync(
            propertyId.Value,
            cancellationToken);

        if (property is null)
        {
            return Fail<PagedResponse<ServiceResponse>>(
                "Property was not found.",
                "PROPERTY_NOT_FOUND",
                404);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await _repository.GetListAsync(
            propertyId.Value,
            request.Search,
            request.IncludeInactive,
            (pageNumber - 1) * pageSize,
            pageSize,
            cancellationToken);

        return Ok(new PagedResponse<ServiceResponse>
        {
            Items = result.Items.Select(Map).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        });
    }

    public async Task<ResponseStatus<ServiceResponse>> GetAsync(
        User actor,
        int propertyId,
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(
            actor,
            propertyId,
            Permissions.Services.Read,
            cancellationToken);

        if (!access.Success)
        {
            return Fail<ServiceResponse>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var service = await _repository.GetByIdAsync(
            propertyId,
            serviceId,
            false,
            cancellationToken);

        return service is null
            ? Fail<ServiceResponse>(
                "Service was not found.",
                "SERVICE_NOT_FOUND",
                404)
            : Ok(Map(service));
    }

    public async Task<ResponseStatus<ServiceResponse>> CreateAsync(
        User actor,
        int propertyId,
        CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(
            actor,
            propertyId,
            Permissions.Services.Create,
            cancellationToken);

        if (!access.Success)
        {
            return Fail<ServiceResponse>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var property = await _repository.GetPropertyAsync(
            propertyId,
            cancellationToken);

        if (property is null)
        {
            return Fail<ServiceResponse>(
                "Property was not found.",
                "PROPERTY_NOT_FOUND",
                404);
        }

        if (property.Status != PropertyStatus.Active)
        {
            return Fail<ServiceResponse>(
                "Services cannot be created under an inactive property.",
                "PROPERTY_INACTIVE",
                409);
        }

        var validationError = ValidateValues(
            request.Name,
            request.RevenueCategory,
            request.Price,
            request.Tax);

        if (validationError is not null)
        {
            return Fail<ServiceResponse>(
                validationError.Value.Message,
                validationError.Value.Code,
                422);
        }

        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(
                propertyId,
                name.ToLowerInvariant(),
                null,
                cancellationToken))
        {
            return Fail<ServiceResponse>(
                "A service with the same name already exists in this property.",
                "SERVICE_ALREADY_EXISTS",
                409);
        }

        var now = DateTimeOffset.UtcNow;
        var service = new Service
        {
            Name = name,
            Description = CleanOptional(request.Description),
            RevenueCategory = CleanOptional(request.RevenueCategory),
            Price = Money(request.Price),
            Tax = Money(request.Tax),
            IsActive = true,
            propertyid = propertyId,
            CreatedAt = now,
            CreatedBy = actor.Id
        };

        try
        {
            await _repository.PersistAsync(
                service,
                CreateAudit(
                    actor,
                    propertyId,
                    service.id.ToString(),
                    "Service.Created",
                    null,
                    service,
                    null),
                cancellationToken);

            return new ResponseStatus<ServiceResponse>(
                Map(service),
                "Service created.",
                201);
        }
        catch (DbUpdateException)
        {
            return Fail<ServiceResponse>(
                "The service conflicts with existing data.",
                "SERVICE_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<ServiceResponse>> UpdateAsync(
        User actor,
        int propertyId,
        int serviceId,
        UpdateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(
            actor,
            propertyId,
            Permissions.Services.Update,
            cancellationToken);

        if (!access.Success)
        {
            return Fail<ServiceResponse>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var service = await _repository.GetByIdAsync(
            propertyId,
            serviceId,
            true,
            cancellationToken);

        if (service is null)
        {
            return Fail<ServiceResponse>(
                "Service was not found.",
                "SERVICE_NOT_FOUND",
                404);
        }

        if (!service.IsActive)
        {
            return Fail<ServiceResponse>(
                "Inactive services cannot be updated.",
                "SERVICE_INACTIVE",
                409);
        }

        var validationError = ValidateValues(
            request.Name,
            request.RevenueCategory,
            request.Price,
            request.Tax);

        if (validationError is not null)
        {
            return Fail<ServiceResponse>(
                validationError.Value.Message,
                validationError.Value.Code,
                422);
        }

        var name = NormalizeName(request.Name);
        if (await _repository.ExistsByNameAsync(
                propertyId,
                name.ToLowerInvariant(),
                serviceId,
                cancellationToken))
        {
            return Fail<ServiceResponse>(
                "A service with the same name already exists in this property.",
                "SERVICE_ALREADY_EXISTS",
                409);
        }

        var oldValues = Map(service);
        service.Name = name;
        service.Description = CleanOptional(request.Description);
        service.RevenueCategory = CleanOptional(request.RevenueCategory);
        service.Price = Money(request.Price);
        service.Tax = Money(request.Tax);
        service.UpdatedAt = DateTimeOffset.UtcNow;
        service.UpdatedBy = actor.Id;

        try
        {
            await _repository.PersistAsync(
                service,
                CreateAudit(
                    actor,
                    propertyId,
                    service.id.ToString(),
                    "Service.Updated",
                    oldValues,
                    service,
                    null),
                cancellationToken);

            return Ok(Map(service), "Service updated.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<ServiceResponse>(
                "The service was modified by another request.",
                "CONCURRENCY_CONFLICT",
                409);
        }
        catch (DbUpdateException)
        {
            return Fail<ServiceResponse>(
                "The service conflicts with existing data.",
                "SERVICE_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<ServiceResponse>> DeactivateAsync(            
        User actor,
        int propertyId,
        int serviceId,
        DeactivateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeAsync(
            actor,
            propertyId,
            Permissions.Services.Deactivate,
            cancellationToken);

        if (!access.Success)
        {
            return Fail<ServiceResponse>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var service = await _repository.GetByIdAsync(
            propertyId,
            serviceId,
            true,
            cancellationToken);

        if (service is null)
        {
            return Fail<ServiceResponse>(
                "Service was not found.",
                "SERVICE_NOT_FOUND",
                404);
        }

        if (!service.IsActive)
        {
            return Fail<ServiceResponse>(
                "Service is already inactive.",
                "SERVICE_ALREADY_INACTIVE",
                409);
        }

        var oldValues = Map(service);
        service.IsActive = false;
        service.UpdatedAt = DateTimeOffset.UtcNow;
        service.UpdatedBy = actor.Id;

        try
        {
            await _repository.PersistAsync(
                service,
                CreateAudit(
                    actor,
                    propertyId,
                    service.id.ToString(),
                    "Service.Deactivated",
                    oldValues,
                    service,
                    request?.Reason),
                cancellationToken);

            return Ok(Map(service), "Service deactivated.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<ServiceResponse>(
                "The service was modified by another request.",
                "CONCURRENCY_CONFLICT",
                409);
        }
    }

    private async Task<ResponseStatus<bool>> AuthorizeAsync(
        User actor,
        int propertyId,
        string permission,
        CancellationToken cancellationToken)
    {
        if (actor is null)
        {
            return Fail<bool>(
                "User is not authenticated.",
                "UNAUTHENTICATED",
                401);
        }

        var roles = await _users.GetRolesAsync(actor.Id, cancellationToken);
        var isAdmin = roles.Any(role =>
            role.IsActive &&
            string.Equals(
                role.Name,
                nameof(UserRole.Admin),
                StringComparison.OrdinalIgnoreCase));

        if (!isAdmin &&
            (!actor.PropertyId.HasValue || actor.PropertyId.Value != propertyId))
        {
            return Fail<bool>(
                "You are not authorized to access this property.",
                "UNAUTHORIZED_PROPERTY",
                403);
        }

        if (isAdmin)
        {
            return Ok(true);
        }

        var permissions = await _users.GetPermissionNamesAsync(
            actor.Id,
            cancellationToken);

        return permissions.Contains(permission)
            ? Ok(true)
            : Fail<bool>(
                "You are not allowed to perform this service operation.",
                "FORBIDDEN_SERVICE_OPERATION",
                403);
    }

    private static (string Code, string Message)? ValidateValues(
        string name,
        string? revenueCategory,
        decimal price,
        decimal tax)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ("INVALID_SERVICE_NAME", "Service name is required.");
        }

        if (string.IsNullOrWhiteSpace(revenueCategory))
        {
            return ("INVALID_SERVICE_REVENUE_CATEGORY", "Revenue category is required.");
        }

        if (price <= 0)
        {
            return ("INVALID_SERVICE_PRICE", "Service price must be greater than zero.");
        }

        if (tax < 0)
        {
            return ("INVALID_SERVICE_TAX", "Service tax cannot be negative.");
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        return string.Join(
            ' ',
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal Money(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static ServiceResponse Map(Service service)
    {
        return new ServiceResponse
        {
            Id = service.id,
            PropertyId = service.propertyid,
            Name = service.Name,
            Description = service.Description,
            RevenueCategory = service.RevenueCategory,
            Price = service.Price,
            Tax = service.Tax,
            IsActive = service.IsActive,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }

    private static AuditLog CreateAudit(
        User actor,
        int propertyId,
        string entityId,
        string action,
        object? oldValues,
        object? newValues,
        string? reason)
    {
        return new AuditLog
        {
            UserId = actor.Id,
            PropertyId = propertyId,
            EntityName = nameof(Service),
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };
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
        string? code,
        int statusCode)
    {
        return new ResponseStatus<T>(
            message: message,
            statusCode: statusCode,
            code: code);
    }
}
