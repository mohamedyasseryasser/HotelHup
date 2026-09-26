using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.folio;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using HotelHup.APPLICATION.Constant;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class FolioService : IFolioService
{
    private readonly IFolioRepository _repository;
    private readonly IUserRepository _users;

    public FolioService(IFolioRepository repository, IUserRepository users)
    {                                                                         
        _repository = repository;
        _users = users;
    }

    public async Task<ResponseStatus<FolioResponseDto>>
        GetAsync(User actor, int folioId, CancellationToken ct = default)
    {
        var folio = await _repository.GetByIdAsync(folioId, false, ct);
        if (folio is null) return Fail<FolioResponseDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
        var access = await AuthorizeAsync(actor, folio, Permissions.Folios.Read, ct);
        return !access.Success ? Fail<FolioResponseDto>(access.Message, access.Code, access.StatusCode) : Ok(Map(folio));
    }

    public async Task<ResponseStatus<FolioResponseDto>> 
        AddItemAsync(User actor,
        int folioId,
        AddFolioItemRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var folio = await _repository.GetForUpdateAsync(folioId, ct);
                {
                    if (folio is null) return Fail<FolioResponseDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
                }   var access = await AuthorizeAsync(actor, folio, Permissions.Folios.AddCharge, ct);
                if (!access.Success)
                {
                    return Fail<FolioResponseDto>(access.Message, access.Code, access.StatusCode);
                }
                if (folio.Status == FolioStatus.Closed)
                {
                    return Fail<FolioResponseDto>("Closed folio cannot accept new items.", "FOLIO_CLOSED", 409);
                }
                if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Source) || request.Amount <= 0 || request.Tax < 0)
                {
                    return Fail<FolioResponseDto>("Amount, tax, type and source are invalid.", "INVALID_FOLIO_AMOUNT", 422);
                }
                if (request.Type is FolioItemType.Payment or FolioItemType.Refund)
                {
                    return Fail<FolioResponseDto>("Payments and refunds must use their dedicated transaction module.", "PAYMENT_MODULE_REQUIRED", 422);
                }
                if (request.Type == FolioItemType.Service && request.Source != FolioItemSource.Service)
                {
                    return Fail<FolioResponseDto>("Service folio items must use Service as their source.", "SERVICE_SOURCE_REQUIRED", 422);
                }
                if (request.Type != FolioItemType.Service && request.ServiceId.HasValue)
                {
                    return Fail<FolioResponseDto>("ServiceId is only valid for service folio items.", "SERVICE_ID_NOT_ALLOWED", 422);
                } if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) && await _repository.IdempotencyKeyExistsAsync(folioId, request.IdempotencyKey, ct))
                {
                    return Fail<FolioResponseDto>("The idempotency key has already been used.", "IDEMPOTENCY_CONFLICT", 409);
                }
                Service? service = null;
                if (request.Type == FolioItemType.Service)
                {
                    if (!request.ServiceId.HasValue)
                    {
                        return Fail<FolioResponseDto>("ServiceId is required for service folio items.", "SERVICE_ID_REQUIRED", 422);
                    }
                    service = await _repository.GetServiceAsync(request.ServiceId.Value, folio.Reservation.PropertyId, ct);
                    if (service is null)
                    {
                        return Fail<FolioResponseDto>("The selected service was not found for this property.", "SERVICE_NOT_FOUND", 404);
                    }
                    if (!service.IsActive)
                    {
                        return Fail<FolioResponseDto>("The selected service is inactive.", "SERVICE_INACTIVE", 409);
                    }
                }

                var now = DateTimeOffset.UtcNow;
                var item = new FolioItem
                {
                    FolioId = folio.id,
                    Type = request.Type,
                    ServiceId = service?.id,
                    ServiceNameSnapshot = service?.Name,
                    Amount = Money(service?.Price ?? request.Amount),
                    Tax = Money(service?.Tax ?? request.Tax),
                    Source = request.Source,
                    Description = request.Description?.Trim() ?? service?.Name,
                    SourceReference = request.IdempotencyKey ?? request.ReferenceId,
                    Status = FolioItemStatus.Posted,
                    PostedAt = now,
                    CreatedAt = now,
                    CreatedBy = actor.Id
                };
                folio.Items.Add(item);
                Recalculate(folio);
                await _repository.SaveAsync(folio, Audit(actor, folio, "FolioItem.Added", null, item, null), ct);
                return Ok(Map(folio), "Folio item added.", 201);
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<FolioResponseDto>("The folio was modified by another request.", "FOLIO_CONCURRENCY_CONFLICT", 409);
        }
    }

    public async Task<ResponseStatus<FolioResponseDto>> 
        VoidItemAsync(
        User actor, 
        int folioId,
        int itemId,
        VoidFolioItemRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var folio = await _repository.GetForUpdateAsync(folioId, ct);
                if (folio is null)
                {
                    return Fail<FolioResponseDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
                } var access = await AuthorizeAsync(actor, folio, Permissions.Folios.Update, ct);
                if (!access.Success)
                {
                    return Fail<FolioResponseDto>(access.Message, access.Code, access.StatusCode);
                }
                if (folio.Status == FolioStatus.Closed)
                {
                    return Fail<FolioResponseDto>("Closed folio cannot be modified.", "FOLIO_CLOSED", 409);
                }
                var item = folio.Items.SingleOrDefault(x => x.id == itemId);
                if (item is null)
                {
                    return Fail<FolioResponseDto>("Folio item was not found.", "FOLIO_ITEM_NOT_FOUND", 404);
                }  if (item.Status != FolioItemStatus.Posted) return Fail<FolioResponseDto>("Folio item is already voided or reversed.", "FOLIO_ITEM_ALREADY_VOIDED", 409);
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return Fail<FolioResponseDto>("A reason is required.", "REASON_REQUIRED", 422);
                }
                if (!MatchesRowVersion(folio, request.ExpectedRowVersion))
                {
                    return Fail<FolioResponseDto>("The folio was modified by another request.", "FOLIO_CONCURRENCY_CONFLICT", 409);
                }
                item.Status = FolioItemStatus.Voided;
                item.UpdatedAt = DateTimeOffset.UtcNow;
                item.UpdatedBy = actor.Id;
                Recalculate(folio);
                await _repository.SaveAsync(folio, Audit(actor, folio, "FolioItem.Voided", item, item, request.Reason), ct);
                return Ok(Map(folio), "Folio item voided.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<FolioResponseDto>("The folio was modified by another request.", "FOLIO_CONCURRENCY_CONFLICT", 409);
        }
    }

    public Task<ResponseStatus<FolioResponseDto>>
        ReopenAsync(
        User actor,
        int folioId,
        ReopenFolioRequestDto request,
        CancellationToken ct = default) =>
        ChangeStatusAsync(actor, 
            folioId, 
            FolioStatus.Open, 
            request.Reason,
            request.ExpectedRowVersion, 
            "Folio.Reopened",
            ct);

    public Task<ResponseStatus<FolioResponseDto>>
        CloseAsync(
        User actor, 
        int folioId,
        CloseFolioRequestDto request,
        CancellationToken ct = default) =>
        ChangeStatusAsync(actor, folioId, FolioStatus.Closed, request.Reason, request.ExpectedRowVersion, "Folio.Closed", ct);

 
    public async Task<ResponseStatus<FolioItemPageDto>> GetItemsAsync(User actor, int folioId, FolioItemPageRequest request, CancellationToken ct = default)
    {
        var folio = await _repository.GetByIdAsync(folioId, false, ct);
        if (folio is null) return Fail<FolioItemPageDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
        var access = await AuthorizeAsync(actor, folio, Permissions.Folios.Read, ct);
        if (!access.Success) return Fail<FolioItemPageDto>(access.Message, access.Code, access.StatusCode);
        var (items, total) = await _repository.GetItemsAsync(folioId, request.PageNumber, request.PageSize, ct);
        return Ok(new FolioItemPageDto { Items = items.Select(Map).ToList(), PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total });
    }

    public async Task<ResponseStatus<FolioSummaryDto>> GetSummaryAsync(User actor, int folioId, CancellationToken ct = default)
    {
        var folio = await _repository.GetByIdAsync(folioId, false, ct);
        if (folio is null) return Fail<FolioSummaryDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
        var access = await AuthorizeAsync(actor, folio, Permissions.Folios.Read, ct);
        if (!access.Success) return Fail<FolioSummaryDto>(access.Message, access.Code, access.StatusCode);
        return Ok(Summary(folio));
    }

   //------------------------------------------------------------------------------------------------
   //private methods
   //------------------------------------------------------------------------------------------------
    private async Task<ResponseStatus<bool>> 
        AuthorizeAsync(
        User actor,
        Folio folio,
        string permission,
        CancellationToken ct)
    {
        if (actor is null)
        {
            return Fail<bool>("User is not authenticated.", "UNAUTHENTICATED", 401);
        }
        var roles = await _users.GetRolesAsync(actor.Id, ct);
        var isAdmin = roles.Any(x => x.IsActive && string.Equals(x.Name, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase));

        if (!isAdmin && (!actor.PropertyId.HasValue || actor.PropertyId.Value != folio.Reservation.PropertyId))
        {
            return Fail<bool>("You are not authorized to access this property.", "FOLIO_PROPERTY_MISMATCH", 403);
        }
        if (isAdmin)
        {
            return Ok(true);
        }
        var permissions = await _users.GetPermissionNamesAsync(actor.Id, ct);
        if (!permissions.Contains(permission))
        {
            return Fail<bool>("You are not allowed to perform this folio operation.", "FORBIDDEN_FOLIO_OPERATION", 403);
        }
        return Ok(true);
    }

    private static void Recalculate(Folio folio)
    {
        var active = folio.Items.Where(x => x.Status == FolioItemStatus.Posted).ToList();
        var charges = active.Where(x => x.Type is FolioItemType.RoomCharge 
        or FolioItemType.Service
        or FolioItemType.Tax
        or FolioItemType.Fee
        or FolioItemType.Adjustment).
        Sum(x => x.Amount);

        var taxes = active.Sum(x => x.Tax);
        var discounts = active.Where(x => x.Type == FolioItemType.Discount).
            Sum(x => x.Amount);

        folio.Total = Money(Math.Max(0m, charges + taxes - discounts));
        var paid = folio.Payments.Where(x => x.Status is PaymentStatus.Paid or PaymentStatus.PartiallyPaid).Sum(x => x.Amount);
        var refunded = folio.Payments.SelectMany(x => x.Refunds).Where(x => x.Status == RefundStatus.Completed).Sum(x => x.Amount);
        var difference = Money(folio.Total - paid + refunded);
        folio.Balance = Math.Max(0m, difference);
        folio.CreditBalance = Math.Max(0m, -difference);
    }

    private static FolioSummaryDto Summary(Folio x)
    {
        var active = x.Items.Where(i => i.Status == FolioItemStatus.Posted).ToList();
        return new FolioSummaryDto { GrossTotal = x.Total, TotalCharges = Money(active.Where(i => i.Type != FolioItemType.Discount).Sum(i => i.Amount)), Discounts = Money(active.Where(i => i.Type == FolioItemType.Discount).Sum(i => i.Amount)), Taxes = Money(active.Sum(i => i.Tax)), Fees = Money(active.Where(i => i.Type == FolioItemType.Fee).Sum(i => i.Amount)), Payments = Money(x.Payments.Where(p => p.Status is PaymentStatus.Paid or PaymentStatus.PartiallyPaid).Sum(p => p.Amount)), Refunds = Money(x.Payments.SelectMany(p => p.Refunds).Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount)), Balance = x.Balance };
    }
    private async Task<ResponseStatus<FolioResponseDto>> ChangeStatusAsync(
        User actor, 
        int folioId, 
        FolioStatus status,
        string reason,
        string? expected, 
        string action,
        CancellationToken ct)
    {
        try
        {
            return await _repository.ExecuteTransactionAsync(async () =>
            {
                var folio = await _repository.GetForUpdateAsync(folioId, ct);
                if (folio is null)
                {
                    return Fail<FolioResponseDto>("Folio was not found.", "FOLIO_NOT_FOUND", 404);
                }
                var permission = status == FolioStatus.Open ? Permissions.Folios.Reopen : Permissions.Folios.Close;
                var access = await AuthorizeAsync(actor, folio, permission, ct);
                if (!access.Success)
                {
                    return Fail<FolioResponseDto>(access.Message, access.Code, access.StatusCode);
                }
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return Fail<FolioResponseDto>("A reason is required.", "REASON_REQUIRED", 422);
                }
                if (!MatchesRowVersion(folio, expected))
                {
                    return Fail<FolioResponseDto>("The folio was modified by another request.", "FOLIO_CONCURRENCY_CONFLICT", 409);
                }
                if (status == folio.Status)
                {
                    return Fail<FolioResponseDto>("Folio is already in the requested state.", "INVALID_FOLIO_STATE", 409);
                }
                var old = new { folio.Status, folio.Total, folio.Balance };
                folio.Status = status;
                folio.UpdatedAt = DateTimeOffset.UtcNow;  
                folio.UpdatedBy = actor.Id;
                if (status == FolioStatus.Closed)
                {
                    folio.ClosedAt = folio.UpdatedAt;
                    folio.ClosedBy = actor.Id;
                }
                else 
                {
                    folio.ClosedAt = null; 
                    folio.ClosedBy = null; 
                }
                await _repository.SaveAsync(folio, Audit(actor, folio, action, old, 
                    new 
                    { folio.Status, folio.Total, folio.Balance }, reason), ct);
                return Ok(Map(folio), $"Folio {status.ToString().ToLowerInvariant()}.");
            }, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fail<FolioResponseDto>("The folio was modified by another request.", "FOLIO_CONCURRENCY_CONFLICT", 409);
        }
    }

    private static FolioResponseDto Map(Folio x) => new() { Id = x.id, ReservationId = x.ReservationId, Status = x.Status, Currency = x.Currency, Total = x.Total, Balance = x.Balance, CreditBalance = x.CreditBalance, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt, RowVersion = Convert.ToBase64String(x.RowVersion ?? Array.Empty<byte>()), Items = x.Items.Select(Map).ToList(), Payments = x.Payments.Select(p => new PaymentResponseDto { Id = p.Id, Amount = p.Amount, Method = p.Method, Status = p.Status, ExternalId = p.ExternalId, PaidAt = p.PaidAt, Refunds = p.Refunds.Select(r => new RefundResponseDto { Id = r.id, PaymentId = r.PaymentId, Amount = r.Amount, Status = r.Status, Reason = r.Reason, ExternalId = r.ExternalId }).ToList() }).ToList(), Refunds = x.Payments.SelectMany(p => p.Refunds.Select(r => new RefundResponseDto { Id = r.id, PaymentId = r.PaymentId, Amount = r.Amount, Status = r.Status, Reason = r.Reason, ExternalId = r.ExternalId })).ToList() };
    private static FolioItemResponseDto
        Map(FolioItem x) => 
        new()
        { 
            Id = x.id,
            FolioId = x.FolioId,
            Type = x.Type, 
            ServiceId = x.ServiceId,
            ServiceName = x.ServiceNameSnapshot, 
            Amount = x.Amount,
            Tax = x.Tax,
            Source = x.Source,
            Status = x.Status, 
            Description = x.Description, 
            ReferenceId = x.SourceReference, 
            PostedAt = x.PostedAt,
            CreatedAt = x.CreatedAt, 
            CreatedBy = x.CreatedBy 
        };
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static bool MatchesRowVersion(Folio folio, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }
        try 
        {
            return Convert.FromBase64String(expected).SequenceEqual(folio.RowVersion ?? Array.Empty<byte>());
        }
        catch (FormatException)
        {
            return false;
        }
    }
    private static AuditLog Audit(User actor, Folio folio, string action, object? oldValues, object? newValues, string? reason) => new() { UserId = actor.Id, PropertyId = folio.Reservation.PropertyId, EntityName = nameof(Folio), EntityId = folio.id.ToString(), Action = action, OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues), NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues), Reason = reason, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = actor.Id };
    private static ResponseStatus<T> Ok<T>(T data, string message = "", int status = 200) => new(data, message, status);
    private static ResponseStatus<T> Fail<T>
        (string message, string? code = null, int status = 400) => new(message: message, statusCode: status, code: code);
   
}
