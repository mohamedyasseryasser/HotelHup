using System.Text.Json;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.Expense;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repo;
    private readonly IUserRepository _users;
    private readonly IHttpContextAccessor _http;

    public ExpenseService(
        IExpenseRepository repo,
        IUserRepository users,
        IHttpContextAccessor http)
    {
        _repo = repo;
        _users = users;
        _http = http;
    }

    public async Task<ResponseStatus<ExpenseResponseDto>> CreateAsync(
        CreateExpenseDto dto,
        User actor,
        CancellationToken ct = default)
    {
        if (dto is null ||
            dto.Amount <= 0 ||
            string.IsNullOrWhiteSpace(dto.Category) ||
            string.IsNullOrWhiteSpace(dto.Description) ||
            string.IsNullOrWhiteSpace(dto.Currency) ||
            dto.ExpenseDate == default ||
            !Enum.IsDefined(dto.PaymentMethod))
        {
            return Fail<ExpenseResponseDto>(
                "Expense data is invalid.",
                "INVALID_EXPENSE",
                422);
        }

        var access = await AuthorizeAsync(
            actor,
            dto.PropertyId,
            Permissions.Expenses.Create,
            ct);

        if (!access.Success)
        {
            return Fail<ExpenseResponseDto>(
                access.Message,
                access.Code,
                access.StatusCode);
        }

        var property = await _users.GetPropertyAsync(
            dto.PropertyId,
            ct);

        if (property is null)
        {
            return Fail<ExpenseResponseDto>(
                "Property was not found.",
                "PROPERTY_NOT_FOUND",
                404);
        }

        if (property.Status == PropertyStatus.Inactive)
        {
            return Fail<ExpenseResponseDto>(
                "Inactive property cannot receive expenses.",
                "PROPERTY_INACTIVE",
                422);
        }

        var now = DateTimeOffset.UtcNow;

        var expense = new Expense
        {
            PropertyId = dto.PropertyId,
            Category = dto.Category.Trim(),
            Description = dto.Description.Trim(),
            Amount = decimal.Round(dto.Amount, 2),
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            ExpenseDate = dto.ExpenseDate,
            PaymentMethod = dto.PaymentMethod,
            VendorName = Clean(dto.VendorName),
            ReferenceNumber = Clean(dto.ReferenceNumber),
            Notes = Clean(dto.Notes),
            Status = ExpenseStatus.Posted,
            CreatedAt = now,
            CreatedBy = actor.Id
        };

        return await _repo.ExecuteTransactionAsync(
            async () =>
            {
                await _repo.AddAsync(
                    expense,
                    Audit(
                        actor,
                        expense,
                        "Expense.Created",
                        null,
                        expense));

                await _repo.SaveChangesAsync(ct);

                return new ResponseStatus<ExpenseResponseDto>(
                    Map(expense),
                    "Expense created successfully.",
                    201,
                    correlationId: CorrelationId());
            },
            ct);
    }

    public async Task<ResponseStatus<PagedResponse<ExpenseResponseDto>>> GetPagedAsync(
        ExpenseQueryDto dto,
        User actor,
        CancellationToken ct = default)
    {
        if (dto.FromDate.HasValue &&
            dto.ToDate.HasValue &&
            dto.FromDate > dto.ToDate)
        {
            return Fail<PagedResponse<ExpenseResponseDto>>(
                "FromDate must be before ToDate.",
                "INVALID_DATE_RANGE",
                422);
        }

        var scope = await ScopeAsync(
            actor,
            dto.PropertyId,
            Permissions.Expenses.Read,
            ct);

        if (!scope.Success)
        {
            return Fail<PagedResponse<ExpenseResponseDto>>(
                scope.Message,
                scope.Code,
                scope.StatusCode);
        }

        var page = Math.Max(1, dto.PageNumber);
        var size = Math.Clamp(dto.PageSize, 1, 100);

        var result = await _repo.GetPagedAsync(
            scope.Data,
            dto.FromDate,
            dto.ToDate,
            dto.Category?.Trim(),
            dto.PaymentMethod,
            dto.Status,
            dto.VendorName?.Trim(),
            page,
            size,
            dto.SortBy,
            dto.Descending,
            ct);

        return new ResponseStatus<PagedResponse<ExpenseResponseDto>>(
            new PagedResponse<ExpenseResponseDto>
            {
                Items = result.Items
                    .Select(Map)
                    .ToList(),

                PageNumber = page,
                PageSize = size,
                TotalCount = result.TotalCount
            },
            "Expenses retrieved successfully.",
            correlationId: CorrelationId());
    }

    public async Task<ResponseStatus<ExpenseResponseDto>> GetByIdAsync(
        int id,
        User actor,
        CancellationToken ct = default)
    {
        var scope = await ScopeAsync(
            actor,
            null,
            Permissions.Expenses.Read,
            ct);

        if (!scope.Success)
        {
            return Fail<ExpenseResponseDto>(
                scope.Message,
                scope.Code,
                scope.StatusCode);
        }

        var expense = await _repo.GetByIdAndPropertyAsync(
            id,
            scope.Data,
            false,
            ct);

        if (expense is null)
        {
            return Fail<ExpenseResponseDto>(
                "Expense was not found.",
                "EXPENSE_NOT_FOUND",
                404);
        }

        return new ResponseStatus<ExpenseResponseDto>(
            Map(expense),
            "Expense retrieved successfully.",
            correlationId: CorrelationId());
    }

    public async Task<ResponseStatus<ExpenseResponseDto>> UpdateAsync(
        int id,
        UpdateExpenseDto dto,
        User actor,
        CancellationToken ct = default)
    {
        var scope = await ScopeAsync(
            actor,
            null,
            Permissions.Expenses.Update,
            ct);

        if (!scope.Success)
        {
            return Fail<ExpenseResponseDto>(
                scope.Message,
                scope.Code,
                scope.StatusCode);
        }

        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.Category) ||
            string.IsNullOrWhiteSpace(dto.Description))
        {
            return Fail<ExpenseResponseDto>(
                "Expense data is invalid.",
                "INVALID_EXPENSE",
                422);
        }

        var expense = await _repo.GetByIdAndPropertyAsync(
            id,
            scope.Data,
            true,
            ct);

        if (expense is null)
        {
            return Fail<ExpenseResponseDto>(
                "Expense was not found.",
                "EXPENSE_NOT_FOUND",
                404);
        }

        if (expense.Status == ExpenseStatus.Voided)
        {
            return Fail<ExpenseResponseDto>(
                "Voided expenses cannot be updated.",
                "EXPENSE_VOIDED",
                409);
        }

        if (!CheckVersion(
                dto.ExpectedRowVersion,
                expense.RowVersion))
        {
            return Fail<ExpenseResponseDto>(
                "The expense was modified by another user.",
                "EXPENSE_CONCURRENCY_CONFLICT",
                409);
        }

        expense.Category = dto.Category.Trim();
        expense.Description = dto.Description.Trim();
        expense.VendorName = Clean(dto.VendorName);
        expense.ReferenceNumber = Clean(dto.ReferenceNumber);
        expense.Notes = Clean(dto.Notes);
        expense.UpdatedAt = DateTimeOffset.UtcNow;
        expense.UpdatedBy = actor.Id;

        try
        {
            return await _repo.ExecuteTransactionAsync(
                async () =>
                {
                    await _repo.AddAuditAsync(
                        Audit(
                            actor,
                            expense,
                            "Expense.Updated",
                            null,
                            expense),
                        ct);

                    await _repo.SaveChangesAsync(ct);

                    return new ResponseStatus<ExpenseResponseDto>(
                        Map(expense),
                        "Expense updated successfully.",
                        correlationId: CorrelationId());
                },
                ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<ExpenseResponseDto>(
                "The expense was modified by another user.",
                "EXPENSE_CONCURRENCY_CONFLICT",
                409);
        }
    }

    public async Task<ResponseStatus<ExpenseResponseDto>> VoidAsync(
        int id,
        VoidExpenseDto dto,
        User actor,
        CancellationToken ct = default)
    {
        var scope = await ScopeAsync(
            actor,
            null,
            Permissions.Expenses.Void,
            ct);

        if (!scope.Success)
        {
            return Fail<ExpenseResponseDto>(
                scope.Message,
                scope.Code,
                scope.StatusCode);
        }

        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.Reason))
        {
            return Fail<ExpenseResponseDto>(
                "A void reason is required.",
                "VOID_REASON_REQUIRED",
                422);
        }

        var expense = await _repo.GetByIdAndPropertyAsync(
            id,
            scope.Data,
            true,
            ct);

        if (expense is null)
        {
            return Fail<ExpenseResponseDto>(
                "Expense was not found.",
                "EXPENSE_NOT_FOUND",
                404);
        }

        if (expense.Status == ExpenseStatus.Voided)
        {
            return Fail<ExpenseResponseDto>(
                "Expense is already voided.",
                "EXPENSE_ALREADY_VOIDED",
                409);
        }

        if (!CheckVersion(
                dto.ExpectedRowVersion,
                expense.RowVersion))
        {
            return Fail<ExpenseResponseDto>(
                "The expense was modified by another user.",
                "EXPENSE_CONCURRENCY_CONFLICT",
                409);
        }

        expense.Status = ExpenseStatus.Voided;
        expense.UpdatedAt = DateTimeOffset.UtcNow;
        expense.UpdatedBy = actor.Id;

        try
        {
            return await _repo.ExecuteTransactionAsync(
                async () =>
                {
                    await _repo.AddAuditAsync(
                        Audit(
                            actor,
                            expense,
                            "Expense.Voided",
                            null,
                            new
                            {
                                expense.Id,
                                expense.Status,
                                Reason = dto.Reason
                            }),
                        ct);

                    await _repo.SaveChangesAsync(ct);

                    return new ResponseStatus<ExpenseResponseDto>(
                        Map(expense),
                        "Expense voided successfully.",
                        correlationId: CorrelationId());
                },
                ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<ExpenseResponseDto>(
                "The expense was modified by another user.",
                "EXPENSE_CONCURRENCY_CONFLICT",
                409);
        }
    }

    private async Task<ResponseStatus<int?>> ScopeAsync(
        User actor,
        int? requested,
        string permission,
        CancellationToken ct)
    {
        var access = await AuthorizeAsync(
            actor,
            requested,
            permission,
            ct);

        if (!access.Success)
        {
            return new ResponseStatus<int?>(
                access.Message,
                statusCode: access.StatusCode,
                code: access.Code);
        }

        return new ResponseStatus<int?>(access.Data);
    }

    private async Task<ResponseStatus<int?>> AuthorizeAsync(
        User actor,
        int? requested,
        string permission,
        CancellationToken ct)
    {
        if (actor is null || !actor.IsActive)
        {
            return new ResponseStatus<int?>(
                "User is inactive or unauthenticated.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        if (actor.PropertyId.HasValue &&
            requested.HasValue &&
            actor.PropertyId != requested)
        {
            return new ResponseStatus<int?>(
                "The requested property is outside your scope.",
                statusCode: 403,
                code: "PROPERTY_FORBIDDEN");
        }

        var permissions = await _users.GetPermissionNamesAsync(
            actor.Id,
            ct);

        if (!permissions.Contains(permission))
        {
            return new ResponseStatus<int?>(
                "The required permission is missing.",
                statusCode: 403,
                code: "PERMISSION_DENIED");
        }

        var roles = await _users.GetRolesAsync(
            actor.Id,
            ct);

        var admin =
            !actor.PropertyId.HasValue &&
            roles.Any(r =>
                r.IsActive &&
                string.Equals(
                    r.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase));

        if (!admin && !actor.PropertyId.HasValue)
        {
            return new ResponseStatus<int?>(
                "A property scope is required.",
                statusCode: 403,
                code: "PROPERTY_SCOPE_REQUIRED");
        }

        return new ResponseStatus<int?>(
            actor.PropertyId ?? requested);
    }

    private AuditLog Audit(
        User actor,
        Expense expense,
        string action,
        object? oldValues,
        object? newValues)
    {
        return new AuditLog
        {
            UserId = actor.Id,
            PropertyId = expense.PropertyId,
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Action = action,

            OldValues = oldValues is null
                ? null
                : JsonSerializer.Serialize(oldValues),

            NewValues = newValues is null
                ? null
                : JsonSerializer.Serialize(newValues),

            CorrelationId = CorrelationId(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };
    }

    private string? CorrelationId()
    {
        return _http.HttpContext?
            .Request
            .Headers["X-Correlation-ID"]
            .FirstOrDefault()
            ?? _http.HttpContext?.TraceIdentifier;
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool CheckVersion(
        string expected,
        byte[] actual)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(expected) &&
                   actual.SequenceEqual(
                       Convert.FromBase64String(expected));
        }
        catch
        {
            return false;
        }
    }

    private static ExpenseResponseDto Map(Expense expense)
    {
        return new ExpenseResponseDto
        {
            Id = expense.Id,
            PropertyId = expense.PropertyId,
            Category = expense.Category,
            Description = expense.Description,
            Amount = expense.Amount,
            Currency = expense.Currency,
            ExpenseDate = expense.ExpenseDate,
            PaymentMethod = expense.PaymentMethod,
            VendorName = expense.VendorName,
            ReferenceNumber = expense.ReferenceNumber,
            Notes = expense.Notes,
            Status = expense.Status,
            CreatedAt = expense.CreatedAt,
            CreatedBy = expense.CreatedBy,

            RowVersion = Convert.ToBase64String(
                expense.RowVersion ?? Array.Empty<byte>())
        };
    }

    private static ResponseStatus<T> Fail<T>(
        string message,
        string? code,
        int status)
    {
        return new ResponseStatus<T>(
            message,
            statusCode: status,
            code: code);
    }
}
