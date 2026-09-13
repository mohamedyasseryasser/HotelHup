using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace HotelHup.APPLICATION.services.implementation
{
    public class PropertyTax : IPropertyTax
    {
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IPropertyTaxRepo _repo { get; }
        public IPropertyRepository PropertyRepository { get; }
        public IUserRepository repository { get; }

        public PropertyTax(IHttpContextAccessor httpContextAccessor,IPropertyTaxRepo repo,
            IUserRepository repository,
            IPropertyRepository propertyRepository)
        {
            HttpContextAccessor = httpContextAccessor;
            _repo = repo;
            PropertyRepository = propertyRepository;
            repository = repository;
        }

        public async Task<
            ResponseStatus<IReadOnlyList<TaxResponse>>>
            GetTaxesAsync(
                User actor,
                int propertyId,
                CancellationToken ct = default)
        {
            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<IReadOnlyList<TaxResponse>>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var taxes =
                await _repo.GetCurrentTaxesAsync(
                    propertyId,
                    DateTimeOffset.UtcNow,
                    ct);

            return Ok<IReadOnlyList<TaxResponse>>(
                taxes
                    .Select(ToResponse)
                    .ToArray());
        }


        public async Task<ResponseStatus<TaxResponse>>
            GetTaxAsync(
                User actor,
                int propertyId,
                int taxId,
                CancellationToken ct = default)
        {
            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<TaxResponse>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var tax =
                await _repo.GetTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found for this property.",
                    404);
            }

            var now =
                DateTimeOffset.UtcNow;

            var isCurrent =
                tax.IsActive &&
                tax.ValidFrom <= now &&
                (
                    tax.ValidTo == null ||
                    now < tax.ValidTo
                );

            if (!isCurrent)
            {
                return Fail<TaxResponse>(
                    "Tax is not active or not effective currently.",
                    409);
            }

            return Ok(
                ToResponse(tax));
        }


        public async Task<ResponseStatus<TaxResponse>>
       CreateTaxAsync(
           User actor,
           int propertyId,
           CreateTaxRequest request,
           CancellationToken ct = default)
        {
            if (request is null)
            {
                return Fail<TaxResponse>(
                    "Request body is required.",
                    400);
            }

            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<TaxResponse>(
                    authorization.Message,
                    authorization.StatusCode);
            }
 

            var dateErrors =
                ValidateTaxDates(
                    request.ValidFrom,
                    request.ValidTo);

            if (dateErrors.Count > 0)
            {
                return Fail<TaxResponse>(
                    "Invalid tax dates.",
                    400,
                    dateErrors);
            }

            var valueErrors =
                ValidateTaxValues(
                    request.Rate,
                    request.Type);

            if (valueErrors.Count > 0)
            {
                return Fail<TaxResponse>(
                    "Invalid tax values.",
                    400,
                    valueErrors);
            }

            var now =
                DateTimeOffset.UtcNow;

            if (request.ValidFrom < now)
            {
                return Fail<TaxResponse>(
                    "ValidFrom cannot be in the past.",
                    400);
            }

            var code =
                request.Code.Trim().ToUpperInvariant();

            var codeExists =
                await _repo.TaxCodeExistsAsync(
                    propertyId,
                    code,
                    null,
                    ct);

            if (codeExists)
            {
                return Fail<TaxResponse>(
                    "Tax code already exists for this property.",
                    409);
            }

            var hasOverlap =
                await _repo.HasTaxOverlapAsync(
                    propertyId,
                    code,
                    request.ValidFrom,
                    request.ValidTo,
                    null,
                    ct);

            if (hasOverlap)
            {
                return Fail<TaxResponse>(
                    "Tax effective dates overlap another active tax.",
                    409);
            }

            var tax =
                new Tax
                {
                    PropertyId = propertyId,

                    Name = request.Name.Trim(),

                    Code = code,

                    Rate = decimal.Round(
                        request.Rate,
                        2,
                        MidpointRounding.AwayFromZero),

                    Type = request.Type,

                    IsInclusive = request.IsInclusive,

                    IsActive = true,

                    ValidFrom =
                        request.ValidFrom.ToUniversalTime(),

                    ValidTo =
                        request.ValidTo?.ToUniversalTime(),

                    CreatedAt = now,

                    CreatedBy = actor.Id
                };

            await using var transaction =
                await PropertyRepository.BeginTransactionAsync(ct);

            try
            {
                await PropertyRepository.AddAsync(tax, ct);

                /*
                 * أول Save للحصول على Id الحقيقي.
                 */
                await PropertyRepository.SaveChangesAsync(ct);

                await PropertyRepository.AddAuditLogAsync(
                    Audit(
                        entity: "Tax",
                        action: "TaxCreated",
                        id: tax.Id.ToString(),
                        oldValue: null,
                        newValue: new
                        {
                            TaxId = tax.Id,
                            PropertyId = propertyId,
                            tax.Name,
                            tax.Code,
                            tax.Rate,
                            tax.Type,
                            tax.IsInclusive,
                            tax.IsActive,
                            tax.ValidFrom,
                            tax.ValidTo
                        },
                        reason: null,
                        propertyId: propertyId),
                    ct);

                await PropertyRepository.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(ct);

                return Fail<TaxResponse>(
                    "Tax could not be created.",
                    409);
            }

            return Created(
                ToResponse(tax));
        }


        public async Task<ResponseStatus<TaxResponse>>
      UpdateTaxAsync(
          User actor,
          int propertyId,
          int taxId,
          UpdateTaxRequest request,
          string ifMatch,
          CancellationToken ct = default)
        {
            if (request is null)
            {
                return Fail<TaxResponse>(
                    "Request body is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<TaxResponse>(
                    "If-Match header is required.",
                    428);
            }

            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<TaxResponse>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var tax =
                await _repo.GetTrackedTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found for this property.",
                    404);
            }

            if (!CheckRowVersion(
                    ifMatch,
                    tax.RowVersion))
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Fail<TaxResponse>(
                    "Reason is required.",
                    400);
            }

            /*
             * نحافظ على القيم القديمة عندما لا يتم إرسال field.
             */
            var newName =
                request.Name is null
                    ? tax.Name
                    : request.Name.Trim();

            var newCode =
                request.Code is null
                    ? tax.Code
                    : request.Code.Trim().ToUpperInvariant();

            var newRate =
                request.Rate ?? tax.Rate;

            var newType =
                request.Type ?? tax.Type;

            var newIsInclusive =
                request.IsInclusive ?? tax.IsInclusive;

            var newValidFrom =
                request.ValidFrom ?? tax.ValidFrom;

            DateTimeOffset? newValidTo;

            if (request.ClearValidTo)
            {
                newValidTo = null;
            }
            else if (request.ValidTo.HasValue)
            {
                newValidTo = request.ValidTo.Value;
            }
            else
            {
                newValidTo = tax.ValidTo;
            }

            var dateErrors =
                ValidateTaxDates(
                    newValidFrom,
                    newValidTo);

            if (dateErrors.Count > 0)
            {
                return Fail<TaxResponse>(
                    "Invalid tax dates.",
                    400,
                    dateErrors);
            }

            var valueErrors =
                ValidateTaxValues(
                    newRate,
                    newType);

            if (valueErrors.Count > 0)
            {
                return Fail<TaxResponse>(
                    "Invalid tax values.",
                    400,
                    valueErrors);
            }

            var codeExists =
                await _repo.TaxCodeExistsAsync(
                    propertyId,
                    newCode,
                    taxId,
                    ct);

            if (codeExists)
            {
                return Fail<TaxResponse>(
                    "Tax code already exists for this property.",
                    409);
            }

            var hasOverlap =
                await _repo.HasTaxOverlapAsync(
                    propertyId,
                    newCode,
                    newValidFrom,
                    newValidTo,
                    taxId,
                    ct);

            if (hasOverlap)
            {
                return Fail<TaxResponse>(
                    "Tax effective dates overlap another active tax.",
                    409);
            }

            var oldValues = new
            {
                tax.Id,
                tax.PropertyId,
                tax.Name,
                tax.Code,
                tax.Rate,
                tax.Type,
                tax.IsInclusive,
                tax.IsActive,
                tax.ValidFrom,
                tax.ValidTo
            };

            tax.Name = newName;
            tax.Code = newCode;
            tax.Rate = decimal.Round(
                newRate,
                2,
                MidpointRounding.AwayFromZero);
            tax.Type = newType;
            tax.IsInclusive = newIsInclusive;
            tax.ValidFrom = newValidFrom.ToUniversalTime();
            tax.ValidTo = newValidTo?.ToUniversalTime();
            tax.UpdatedAt = DateTimeOffset.UtcNow;
            tax.UpdatedBy = actor.Id;

            var newValues = new
            {
                tax.Id,
                tax.PropertyId,
                tax.Name,
                tax.Code,
                tax.Rate,
                tax.Type,
                tax.IsInclusive,
                tax.IsActive,
                tax.ValidFrom,
                tax.ValidTo
            };

            await PropertyRepository.AddAuditLogAsync(
                Audit(
                    entity: "Tax",
                    action: "TaxUpdated",
                    id: tax.Id.ToString(),
                    oldValue: oldValues,
                    newValue: newValues,
                    reason: request.Reason.Trim(),
                    propertyId: propertyId),
                ct);

            try
            {
                await PropertyRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<TaxResponse>(
                    "Tax could not be updated.",
                    409);
            }

            return Ok(
                ToResponse(tax));
        }
        public async Task<ResponseStatus<TaxResponse>>
    ActivateTaxAsync(
        User actor,
        int propertyId,
        int taxId,
        string ifMatch,
        CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<TaxResponse>(
                    "If-Match header is required.",
                    428);
            }

            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<TaxResponse>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var tax =
                await _repo.GetTrackedTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found for this property.",
                    404);
            }

            if (!CheckRowVersion(
                    ifMatch,
                    tax.RowVersion))
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            if (tax.IsActive)
            {
                return Fail<TaxResponse>(
                    "Tax is already active.",
                    409);
            }

            var now =
                DateTimeOffset.UtcNow;

            if (tax.ValidTo.HasValue &&
                now >= tax.ValidTo.Value)
            {
                return Fail<TaxResponse>(
                    "Expired tax cannot be activated.",
                    409);
            }

            var hasOverlap =
                await _repo.HasTaxOverlapAsync(
                    propertyId,
                    tax.Code,
                    tax.ValidFrom,
                    tax.ValidTo,
                    tax.Id,
                    ct);

            if (hasOverlap)
            {
                return Fail<TaxResponse>(
                    "Tax cannot be activated because its effective period overlaps another active tax.",
                    409);
            }

            var oldValues = new
            {
                tax.Id,
                tax.IsActive
            };

            tax.IsActive = true;
            tax.UpdatedAt = now;
            tax.UpdatedBy = actor.Id;

            var newValues = new
            {
                tax.Id,
                tax.IsActive
            };

            await PropertyRepository.AddAuditLogAsync(
                Audit(
                    entity: "Tax",
                    action: "TaxActivated",
                    id: tax.Id.ToString(),
                    oldValue: oldValues,
                    newValue: newValues,
                    reason: "Tax activated.",
                    propertyId: propertyId),
                ct);

            try
            {
                await PropertyRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(
                ToResponse(tax));
        }


        public async Task<ResponseStatus<TaxResponse>>
      DeactivateTaxAsync(
          User actor,
          int propertyId,
          int taxId,
          string ifMatch,
          ChangeTaxStatusRequest request,
          CancellationToken ct = default)
        {
            if (request is null)
            {
                return Fail<TaxResponse>(
                    "Request body is required.",
                    400);
            }

            if (request.Confirm != true)
            {
                return Fail<TaxResponse>(
                    "Confirm must be true before deactivating a tax.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Fail<TaxResponse>(
                    "Reason is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<TaxResponse>(
                    "If-Match header is required.",
                    428);
            }

            var authorization =
                await AuthorizeActivePropertyAsync(
                    actor,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<TaxResponse>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var tax =
                await _repo.GetTrackedTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found for this property.",
                    404);
            }

            if (!CheckRowVersion(
                    ifMatch,
                    tax.RowVersion))
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            if (!tax.IsActive)
            {
                return Fail<TaxResponse>(
                    "Tax is already inactive.",
                    409);
            }

            var otherActiveTaxes =
                await _repo.CountActiveTaxesAsync(
                    propertyId,
                    tax.Id,
                    ct);

            if (otherActiveTaxes == 0)
            {
                return Fail<TaxResponse>(
                    "The last active tax cannot be deactivated.",
                    409,
                    new List<string>
                    {
                "LAST_ACTIVE_TAX_CANNOT_BE_DEACTIVATED"
                    });
            }

            /*
             * Many-to-many usage check:
             * Reservation -> ReservationTaxSnapshot -> Tax
             */
            var activeReservationsUsingTax =
                await _repo.CountActiveReservationsUsingTaxAsync(
                    propertyId,
                    tax.Id,
                    ct);

            if (activeReservationsUsingTax > 0)
            {
                return Fail<TaxResponse>(
                    "Tax is used by active reservations and cannot be deactivated.",
                    409,
                    new List<string>
                    {
                "ACTIVE_RESERVATIONS_USE_TAX",
                $"activeReservationsCount={activeReservationsUsingTax}"
                    });
            }

            var oldValues = new
            {
                tax.Id,
                tax.PropertyId,
                tax.IsActive
            };

            tax.IsActive = false;
            tax.UpdatedAt = DateTimeOffset.UtcNow;
            tax.UpdatedBy = actor.Id;

            var newValues = new
            {
                tax.Id,
                tax.PropertyId,
                tax.IsActive
            };

            await PropertyRepository.AddAuditLogAsync(
                Audit(
                    entity: "Tax",
                    action: "TaxDeactivated",
                    id: tax.Id.ToString(),
                    oldValue: oldValues,
                    newValue: newValues,
                    reason: request.Reason.Trim(),
                    propertyId: propertyId),
                ct);

            try
            {
                await PropertyRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(
                ToResponse(tax));
        }


        //private method
        private static TaxResponse ToResponse(
      Tax tax)
        {
            return new TaxResponse
            {
                Id = tax.Id,
                PropertyId = tax.PropertyId,
                Name = tax.Name,
                Code = tax.Code,
                Rate = tax.Rate,
                Type = tax.Type,
                IsInclusive = tax.IsInclusive,
                IsActive = tax.IsActive,
                ValidFrom = tax.ValidFrom,
                ValidTo = tax.ValidTo,

                RowVersion =
                    Convert.ToBase64String(
                        tax.RowVersion
                        ?? Array.Empty<byte>()),

                CreatedAt = tax.CreatedAt,
                CreatedBy = tax.CreatedBy,
                UpdatedAt = tax.UpdatedAt,
                UpdatedBy = tax.UpdatedBy
            };
        }

        private static ResponseStatus<T> Ok<T>(T value)
        {
            return new ResponseStatus<T>(
                value,
                statusCode: 200);
        }

        private static ResponseStatus<T> Created<T>(T value)
        {
            return new ResponseStatus<T>(
                value,
                statusCode: 201);
        }

        private static ResponseStatus<T> Fail<T>(
            string message,
            int statusCode,
            List<string>? errors = null)
        {
            return new ResponseStatus<T>(
                message,
                errors: errors,
                statusCode: statusCode);
        }
        private static ResponseStatus<T> Fail<T>(
         ResponseStatus<bool> response)
        {
            return new ResponseStatus<T>(
                response.Message,
                errors: response.Errors,
                statusCode: response.StatusCode);
        }
        private static bool CheckRowVersion(
      string expectedRowVersion,
      byte[] actualRowVersion)
        {
            if (string.IsNullOrWhiteSpace(expectedRowVersion))
            {
                return false;
            }

            byte[] expectedBytes;

            try
            {
                expectedBytes =
                    Convert.FromBase64String(expectedRowVersion);
            }
            catch (FormatException)
            {
                return false;
            }

            return expectedBytes.SequenceEqual(
                actualRowVersion ?? Array.Empty<byte>());
        }

        private async Task<ResponseStatus<AuthorizationDataDto>> IsAdminAsync(
           string actorId,
           CancellationToken cancellationToken = default)
        {
            var actor = await repository.GetByIdAsync(
                actorId,
                cancellationToken);

            if (actor is null)
            {
                return new ResponseStatus<AuthorizationDataDto>(
                    message: "User not found.",
                    statusCode: 404);
            }

            var roles = await repository.GetRolesAsync(
                actor.Id,
                cancellationToken);

            var isAdmin = roles.Any(role =>
                role.IsActive &&
                string.Equals(
                    role.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase));

            return new ResponseStatus<AuthorizationDataDto>(
                data: new AuthorizationDataDto
                {
                    Actor = actor,
                    Roles = roles,
                    IsAdmin = isAdmin
                },
                statusCode: 200);
        }
        private async Task<ResponseStatus<bool>> AuthorizeAsync(
        string actorId,
        int? propertyId,
        CancellationToken cancellationToken = default)
        {
            var result = await IsAdminAsync(
                actorId,
                cancellationToken);

            if (!result.Success)
            {
                return new ResponseStatus<bool>(
                    message: result.Message,
                    statusCode: result.StatusCode);
            }

            if (result.Data is null)
            {
                return new ResponseStatus<bool>
                    (message: "Authorization data not found.",
                    statusCode: 403);
            }
            var data = result.Data;
            // Admin → Global access
            if (data.IsAdmin)
            {
                return new ResponseStatus<bool>
                    (data: true,
                    statusCode: 200);
            }

            // Check Manager
            var isManager = data.Roles.Any(role =>
                role.IsActive &&
                string.Equals(
                    role.Name,
                    nameof(UserRole.Manager),
                    StringComparison.OrdinalIgnoreCase));

            if (!isManager)
            {
                return new ResponseStatus<bool>(
                    message: "Admin or Manager access required.",
                    statusCode: 403);
            }

            // Manager must belong to a property
            if (!data.Actor.PropertyId.HasValue)
            {
                return new ResponseStatus<bool>(
                    message: "Manager is not assigned to a property.",
                    statusCode: 403);
            }

            // Manager can access only his property
            if (!propertyId.HasValue ||
                data.Actor.PropertyId.Value != propertyId.Value)
            {
                return new ResponseStatus<bool>(
                    message: "You are not authorized to access this property.",
                    statusCode: 403);
            }

            return new ResponseStatus<bool>(
                data: true,
                statusCode: 200);
        }
        private async Task<ResponseStatus<Property>>
    AuthorizeActivePropertyAsync(
        User actor,
        int propertyId,
        CancellationToken ct)
        {
            if (actor is null)
            {
                return Fail<Property>(
                    "Authenticated user is required.",
                    401);
            }

            var authorization =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<Property>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var property =
                await PropertyRepository.GetByIdAsync(
                    propertyId,
                    ct);

            if (property is null)
            {
                return Fail<Property>(
                    "Property not found.",
                    404);
            }

            if (property.Status == PropertyStatus.Inactive)
            {
                return Fail<Property>(
                    "Tax operation is not allowed for an inactive property.",
                    409);
            }

            return Ok(property);
        }

        private AddAuditLogDto Audit(
            string entity,
            string action,
            string? id,
            object? oldValue,
            object? newValue,
            string? reason = null,
            int? propertyId = null, string? actorid = null)
        {
            return new AddAuditLogDto
            {
                userid =
                    actorid ?? "system",

                TargetEntity =
                    entity,

                TargetEntityId =
                    id ?? "pending",

                PropertyId =
                    propertyId,

                Action =
                    action,

                OldValues =
                    oldValue,

                NewValues =
                    newValue,

                Reason =
                    reason,

                CorrelationId =
                    GetCorrelationId()
            };
        }

        private static string Clean(
            string value)
        {
            return value.Trim();
        }

        private static string? CleanOptional(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string Normalize(
            string value)
        {
            return value
                .Trim()
                .ToUpperInvariant();
        }

        private static string NormalizeCurrency(
            string value)
        {
            return value
                .Trim()
                .ToUpperInvariant();
        }

        private static bool ValidZone(
            string zone)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(
                    zone.Trim());

                return true;
            }
            catch
            {
                return false;
            }
        }


        private static List<string>
      ValidateTaxDates(
          DateTimeOffset validFrom,
          DateTimeOffset? validTo)
        {
            var errors = new List<string>();

            if (validFrom == default)
            {
                errors.Add(
                    "ValidFrom is required.");
            }

            if (validTo.HasValue &&
                validTo.Value <= validFrom)
            {
                errors.Add(
                    "ValidTo must be greater than ValidFrom.");
            }

            return errors;
        }


        private static List<string>
    ValidateTaxValues(
        decimal rate,
        TaxType type)
        {
            var errors = new List<string>();

            if (rate < 0)
            {
                errors.Add(
                    "Rate cannot be negative.");
            }

            if (type == TaxType.Percentage &&
                rate > 100)
            {
                errors.Add(
                    "Percentage tax rate must be between 0 and 100.");
            }

            if (decimal.Round(
                    rate,
                    2,
                    MidpointRounding.AwayFromZero) != rate)
            {
                errors.Add(
                    "Rate cannot contain more than 2 decimal places.");
            }

            return errors;
        }


        private async Task<ResponseStatus<TaxResponse>>
    SetTax(
       User actor, int propertyId,
        int taxId,
        bool active,
        CancellationToken ct)
        {
            var auth = await AuthorizeAsync(actor.Id, propertyId, ct);

            if (!auth.Success)
            {
                return Fail<TaxResponse>(auth);
            }

            var tax =
                await _repo.GetTrackedTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found.",
                    404);
            }

            tax.IsActive = active;
            tax.UpdatedAt =
                DateTimeOffset.UtcNow;

            tax.UpdatedBy =
                actor.Id;

            await PropertyRepository.AddAuditLogAsync(
                Audit(
                    "Tax",
                    active
                        ? "Activate"
                        : "Deactivate",
                    taxId.ToString(),
                    null,
                    new
                    {
                        tax.IsActive
                    },
                    null,
                    propertyId),
                ct);
            try
            {
                await PropertyRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<TaxResponse>(
                    "concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(tax));
        }

        private string GetCorrelationId()
        {
            var httpContext =  HttpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                return Guid.NewGuid().ToString();
            }

            var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return httpContext.TraceIdentifier;
        }
    }
}
