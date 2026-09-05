using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class PropertyService : IPropertyService
    {
        private readonly IUserRepository repository;
        private readonly IPropertyRepository _repo;
         private readonly UserManager<User> _users;

        public PropertyService(IUserRepository _repository,
            IPropertyRepository repo,
             UserManager<User> users)
        {
            repository = _repository;
            _repo = repo;
             _users = users;
        }

        public async Task<ResponseStatus<PropertyListResponse>> GetListAsync(
           User CurrentUserLogin, PropertyListRequest r,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(CurrentUserLogin.Id, null, ct);

            if (!auth.Success)
            {
                return Fail<PropertyListResponse>(auth);
            }

            r ??= new();

            var page = Math.Max(1, r.Page);
            var size = Math.Clamp(r.PageSize, 1, 100);

            var items = await _repo.GetListAsync(
                r.Search,
                r.Status,
                r.SortBy,
                r.Descending,
                (page - 1) * size,
                size,
                ct);

            var total = await _repo.CountAsync(
                r.Search,
                r.Status,
                ct);

            return Ok(
                new PropertyListResponse
                {
                    Items = items.Select(ToResponse).ToArray(),
                    Page = page,
                    PageSize = size,
                    TotalCount = total
                });
        }

        public async Task<ResponseStatus<PropertyResponse>> GetAsync(
          User CurrentUserLogin,  int id,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(CurrentUserLogin.Id, id, ct);

            if (!auth.Success)
            {
                return Fail<PropertyResponse>(auth);
            }

            var property = await _repo.GetByIdAsync(id, ct);

            if (property is null)
            {
                return Fail<PropertyResponse>(
                    "Property not found.",
                    404);
            }

            return Ok(ToResponse(property));
        }

        public async Task<ResponseStatus<PropertyResponse>> CreateAsync(
           User CurrentUserLogin, CreatePropertyRequest r,
            CancellationToken ct = default)
        {
            //check currentuserlogin is admin or manager
            var auth = await AuthorizeAsync(CurrentUserLogin.Id,null, ct);

            if (!auth.Success)
            {
                return Fail<PropertyResponse>(auth);
            }
            //validation property
            var errors = ValidateProperty(r);

            if (errors.Count > 0)
            {
                return Fail<PropertyResponse>(
                    "Invalid property.",
                    400,
                    errors);
            }

            var code = Normalize(r.Code);

            if (await _repo.CodeExistsAsync(code, null, ct))
            {
                return Fail<PropertyResponse>(
                    "Property code already exists.",
                    409);
            }

            var now = DateTimeOffset.UtcNow;
            var actorId = CurrentUserLogin.Id;

            var property = new Property
            {
                Name = Clean(r.Name),
                Code = code,
                Description = CleanOptional(r.Description),
                Status = PropertyStatus.Active,
                 CreatedAt = now,
                CreatedBy = actorId
            };

            var settings = new PropertySettings
            {
                Property = property,
                CheckInTime = r.Settings.CheckInTime,
                CheckOutTime = r.Settings.CheckOutTime,
                AllowEarlyCheckIn = r.Settings.AllowEarlyCheckIn,
                HotelDayClosingTime = r.Settings.HotelDayClosingTime,
                DefaultCurrency = NormalizeCurrency(r.Settings.Currency),
                TimeZone = r.Settings.TimeZone.Trim(),
                RequireDepositForReservation = r.Settings.RequireDepositForReservation,
                RequireFullPaymentBeforeCheckOut =
                    r.Settings.RequireFullPaymentBeforeCheckOut,
                AllowOverpayment = r.Settings.AllowOverpayment,
                RequireInspectionBeforeAvailable =
                    r.Settings.RequireInspectionBeforeAvailable,
                CreatedAt = now,
                CreatedBy = actorId
            };

            property.Settings = settings;

            var created = await _repo.CreateAsync(
         property,
         settings,
         Audit(
             "Property",
             "Create",
             null,
             new { property.Name, property.Code },
             null),
        ct);

            return Created(ToResponse(created));

        }

        public async Task<ResponseStatus<PropertyResponse>> UpdateAsync(
           User CurrentUserLogin ,int id,
            UpdatePropertyRequest r,
            CancellationToken ct = default)
        {
            //check currentuserlogin is admin or manager
            var auth = await AuthorizeAsync(CurrentUserLogin.Id,id,ct);
             if (!auth.Success)
             {
                return Fail<PropertyResponse>(auth);
             }
            var property = await _repo.GetTrackedByIdAsync(id, ct);

            if (property is null)
            {
                return Fail<PropertyResponse>(
                    "Property not found.",
                    404);
            }
            if (property.Status==PropertyStatus.Inactive)
            {
                return Fail<PropertyResponse>(
                    "Property not active.",
                    404);
            }
            if (!CheckVersion(
                    r.ExpectedRowVersion,
                    property.Settings.RowVersion))
            {
                return Fail<PropertyResponse>(
                    "Concurrency conflict.",
                    409);
            }

            var old = new
            {
                property.Name,
                property.Code,
                property.Description
            };

            if (r.Name is not null)
            {
                property.Name = Clean(r.Name);
            }

            if (r.Code is not null)
            {
                var code = Normalize(r.Code);

                if (await _repo.CodeExistsAsync(code, id, ct))
                {
                    return Fail<PropertyResponse>(
                        "Property code already exists.",
                        409);
                }

                property.Code = code;
            }

            if (r.Description is not null)
            {
                property.Description =
                    CleanOptional(r.Description);
            }

            property.UpdatedAt = DateTimeOffset.UtcNow;
            property.UpdatedBy = CurrentUserLogin.Id;

            var updated = await _repo.UpdateAsync(
                property,
           Audit(
         "Property",
         "Update",
         id.ToString(),
         old,
         new { property.Name, property.Code, property.Description },
         r.Reason,
         id),
     ct);

            if (!updated)
            {
                return Fail<PropertyResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(property));
        }

        public async Task<ResponseStatus<PropertyResponse>> ActivateAsync(
            int id,
            CancellationToken ct = default)
        {
            return await SetPropertyStatus(
                id,
                PropertyStatus.Active,
                null,
                ct);
        }

        public async Task<ResponseStatus<PropertyResponse>> DeactivateAsync(
            int id,
            DeactivatePropertyRequest r,
            CancellationToken ct = default)
        {
            return await SetPropertyStatus(
                id,
                PropertyStatus.Inactive,
                r?.Reason,
                ct);
        }

        public async Task<ResponseStatus<PropertySettingsResponse>>
            GetSettingsAsync(
                int id,
                CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(id, false, ct);

            if (!auth.Success)
            {
                return Fail<PropertySettingsResponse>(auth);
            }

            var settings = await _repo.GetSettingsAsync(id, ct);

            if (settings is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Settings not found.",
                    404);
            }

            return Ok(ToResponse(settings));
        }

        public async Task<ResponseStatus<PropertySettingsResponse>>
            UpdateSettingsAsync(
                int id,
                UpdatePropertySettingsRequest r,
                CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(id, true, ct);

            if (!auth.Success)
            {
                return Fail<PropertySettingsResponse>(auth);
            }

            var property =
                await _repo.GetTrackedByIdAsync(id, ct);

            if (property is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Property not found.",
                    404);
            }

            var settings =
                await _repo.GetTrackedSettingsAsync(id, ct);

            if (settings is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Settings not found.",
                    404);
            }

            if (!CheckVersion(
                    r.ExpectedRowVersion,
                    settings.RowVersion))
            {
                return Fail<PropertySettingsResponse>(
                    "Concurrency conflict.",
                    409);
            }

            if (r.CheckInTime.HasValue)
            {
                settings.CheckInTime =
                    r.CheckInTime.Value;
            }

            if (r.CheckOutTime.HasValue)
            {
                settings.CheckOutTime =
                    r.CheckOutTime.Value;
            }

            if (r.AllowEarlyCheckIn.HasValue)
            {
                settings.AllowEarlyCheckIn =
                    r.AllowEarlyCheckIn.Value;
            }

            if (r.HotelDayClosingTime.HasValue)
            {
                settings.HotelDayClosingTime =
                    r.HotelDayClosingTime;
            }

            if (r.DefaultCurrency is not null)
            {
                var currency =
                    NormalizeCurrency(r.DefaultCurrency);

                if (
                    await _repo.HasFinancialTransactionsAsync(
                        id,
                        ct)
                    &&
                    !string.Equals(
                        settings.DefaultCurrency,
                        currency,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Fail<PropertySettingsResponse>(
                        "Currency cannot be changed after financial transactions without an administrative procedure.",
                        400);
                }

                settings.DefaultCurrency = currency;
                property.Currency = currency;
            }

            if (r.TimeZone is not null)
            {
                if (!ValidZone(r.TimeZone))
                {
                    return Fail<PropertySettingsResponse>(
                        "Invalid time zone.",
                        400);
                }

                settings.TimeZone = r.TimeZone.Trim();
                property.TimeZone = settings.TimeZone;
            }

            if (r.RequireDepositForReservation.HasValue)
            {
                settings.RequireDepositForReservation =
                    r.RequireDepositForReservation.Value;
            }

            if (r.RequireFullPaymentBeforeCheckOut.HasValue)
            {
                settings.RequireFullPaymentBeforeCheckOut =
                    r.RequireFullPaymentBeforeCheckOut.Value;
            }

            if (r.AllowOverpayment.HasValue)
            {
                settings.AllowOverpayment =
                    r.AllowOverpayment.Value;
            }

            if (r.RequireInspectionBeforeAvailable.HasValue)
            {
                settings.RequireInspectionBeforeAvailable =
                    r.RequireInspectionBeforeAvailable.Value;
            }

            settings.UpdatedAt =
                DateTimeOffset.UtcNow;

            settings.UpdatedBy =
                ActorId();

            await _repo.AddAuditLogAsync(
                Audit(
                    "PropertySettings",
                    "Update",
                    id.ToString(),
                    null,
                    settings,
                    r.Reason),
                ct);

            if (!await Save(ct))
            {
                return Fail<PropertySettingsResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(settings));
        }

        // ============================================================
        // Taxes
        // ============================================================

        public async Task<ResponseStatus<IReadOnlyList<TaxResponse>>>
            GetTaxesAsync(
                int propertyId,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(propertyId, false, ct);

            if (!auth.Success)
            {
                return Fail<IReadOnlyList<TaxResponse>>(auth);
            }

            var taxes =
                await _repo.GetTaxesAsync(propertyId, ct);

            return Ok<IReadOnlyList<TaxResponse>>(
                taxes
                    .Select(ToResponse)
                    .ToArray());
        }

        public async Task<ResponseStatus<TaxResponse>> GetTaxAsync(
            int propertyId,
            int taxId,
            CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(propertyId, false, ct);

            if (!auth.Success)
            {
                return Fail<TaxResponse>(auth);
            }

            var tax =
                await _repo.GetTaxAsync(
                    propertyId,
                    taxId,
                    ct);

            if (tax is null)
            {
                return Fail<TaxResponse>(
                    "Tax not found.",
                    404);
            }

            return Ok(ToResponse(tax));
        }

        public async Task<ResponseStatus<TaxResponse>> CreateTaxAsync(
            int propertyId,
            CreateTaxRequest r,
            CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<TaxResponse>(auth);
            }

            var errors =
                ValidateDates(
                    r.ValidFrom,
                    r.ValidTo);

            if (
                errors.Count > 0
                || r.Rate < 0
                || (
                    r.Type == TaxType.Percentage
                    && r.Rate > 100))
            {
                return Fail<TaxResponse>(
                    "Invalid tax.",
                    400,
                    errors);
            }

            var code = Normalize(r.Code);

            var taxes =
                await _repo.GetTaxesAsync(
                    propertyId,
                    ct);

            if (taxes.Any(x => x.Code == code))
            {
                return Fail<TaxResponse>(
                    "Tax code already exists in this property.",
                    409);
            }

            var tax = new Tax
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Code = code,
                Rate = r.Rate,
                Type = r.Type,
                IsInclusive = r.IsInclusive,
                ValidFrom = r.ValidFrom.ToUniversalTime(),
                ValidTo = r.ValidTo?.ToUniversalTime(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = ActorId()
            };

            await _repo.AddAsync(tax, ct);

            await _repo.AddAuditLogAsync(
                Audit(
                    "Tax",
                    "Create",
                    tax.Id.ToString(),
                    null,
                    tax,
                    null,
                    propertyId),
                ct);

            await _repo.SaveChangesAsync(ct);

            return Created(ToResponse(tax));
        }

        public async Task<ResponseStatus<TaxResponse>> UpdateTaxAsync(
            int propertyId,
            int taxId,
            UpdateTaxRequest r,
            CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

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

            if (
                (r.Rate.HasValue && r.Rate < 0)
                ||
                (
                    r.Type == TaxType.Percentage
                    && r.Rate > 100))
            {
                return Fail<TaxResponse>(
                    "Invalid tax rate.",
                    400);
            }

            if (r.Name is not null)
            {
                tax.Name = Clean(r.Name);
            }

            if (r.Code is not null)
            {
                var code = Normalize(r.Code);

                var taxes =
                    await _repo.GetTaxesAsync(
                        propertyId,
                        ct);

                if (taxes.Any(
                        t => t.Id != taxId
                             && t.Code == code))
                {
                    return Fail<TaxResponse>(
                        "Tax code already exists in this property.",
                        409);
                }

                tax.Code = code;
            }

            if (r.Rate.HasValue)
            {
                tax.Rate = r.Rate.Value;
            }

            if (r.Type.HasValue)
            {
                tax.Type = r.Type.Value;
            }

            if (r.IsInclusive.HasValue)
            {
                tax.IsInclusive =
                    r.IsInclusive.Value;
            }

            if (r.ValidFrom.HasValue)
            {
                tax.ValidFrom =
                    r.ValidFrom.Value.ToUniversalTime();
            }

            if (r.ValidTo.HasValue)
            {
                tax.ValidTo =
                    r.ValidTo.Value.ToUniversalTime();
            }

            var dateErrors =
                ValidateDates(
                    tax.ValidFrom,
                    tax.ValidTo);

            if (dateErrors.Count > 0)
            {
                return Fail<TaxResponse>(
                    "Invalid tax dates.",
                    400,
                    dateErrors);
            }

            tax.UpdatedAt =
                DateTimeOffset.UtcNow;

            tax.UpdatedBy =
                ActorId();

            await _repo.AddAuditLogAsync(
                Audit(
                    "Tax",
                    "Update",
                    taxId.ToString(),
                    null,
                    tax,
                    r.Reason,
                    propertyId),
                ct);

            if (!await Save(ct))
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(tax));
        }

        public async Task<ResponseStatus<TaxResponse>>
            SetTaxStatusAsync(
                int propertyId,
                int taxId,
                bool active,
                CancellationToken ct = default)
        {
            return await SetTax(
                propertyId,
                taxId,
                active,
                ct);
        }

        // ============================================================
        // Cancellation Policies
        // ============================================================

        public async Task<
            ResponseStatus<IReadOnlyList<CancellationPolicyResponse>>>
            GetCancellationPoliciesAsync(
                int propertyId,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    false,
                    ct);

            if (!auth.Success)
            {
                return Fail<
                    IReadOnlyList<CancellationPolicyResponse>>(auth);
            }

            var policies =
                await _repo.GetCancellationPoliciesAsync(
                    propertyId,
                    ct);

            return Ok<
                IReadOnlyList<CancellationPolicyResponse>>(
                policies
                    .Select(ToResponse)
                    .ToArray());
        }

        public async Task<
            ResponseStatus<CancellationPolicyResponse>>
            GetCancellationPolicyAsync(
                int propertyId,
                int policyId,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    false,
                    ct);

            if (!auth.Success)
            {
                return Fail<CancellationPolicyResponse>(auth);
            }

            var policy =
                await _repo.GetCancellationPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (policy is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Cancellation policy not found.",
                    404);
            }

            return Ok(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<CancellationPolicyResponse>>
            CreateCancellationPolicyAsync(
                int propertyId,
                CreateCancellationPolicyRequest r,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<CancellationPolicyResponse>(auth);
            }

            var errors =
                ValidatePolicy(
                    r.ValidFrom,
                    r.ValidTo,
                    r.CancellationFeePercentage,
                    r.FreeCancellationHours,
                    r.CutoffHours,
                    r.Rules);

            if (errors.Count > 0)
            {
                return Fail<CancellationPolicyResponse>(
                    "Invalid cancellation policy.",
                    400,
                    errors);
            }

            var policy = new CancellationPolicy
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Description = CleanOptional(r.Description),
                ValidFrom = r.ValidFrom.ToUniversalTime(),
                ValidTo = r.ValidTo?.ToUniversalTime(),
                Rules = r.Rules,
                Version =
                    await _repo.GetNextCancellationVersionAsync(
                        propertyId,
                        ct),
                FreeCancellationHours =
                    r.FreeCancellationHours,
                CancellationFeePercentage =
                    r.CancellationFeePercentage,
                FixedCancellationFee =
                    r.FixedCancellationFee,
                IsNonRefundable =
                    r.IsNonRefundable,
                CutoffHours =
                    r.CutoffHours,
                CreatedAt =
                    DateTimeOffset.UtcNow,
                CreatedBy =
                    ActorId()
            };

            await _repo.AddAsync(policy, ct);

            await _repo.AddAuditLogAsync(
                Audit(
                    "CancellationPolicy",
                    "Create",
                    policy.Id.ToString(),
                    null,
                    policy,
                    null,
                    propertyId),
                ct);

            await _repo.SaveChangesAsync(ct);

            return Created(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<CancellationPolicyResponse>>
            UpdateCancellationPolicyAsync(
                int propertyId,
                int policyId,
                UpdateCancellationPolicyRequest r,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<CancellationPolicyResponse>(auth);
            }

            var old =
                await _repo.GetCancellationPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (old is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Cancellation policy not found.",
                    404);
            }

            var errors =
                ValidatePolicy(
                    r.ValidFrom,
                    r.ValidTo,
                    r.CancellationFeePercentage,
                    r.FreeCancellationHours,
                    r.CutoffHours,
                    r.Rules);

            if (errors.Count > 0)
            {
                return Fail<CancellationPolicyResponse>(
                    "Invalid cancellation policy.",
                    400,
                    errors);
            }

            var policy = new CancellationPolicy
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Description = CleanOptional(r.Description),
                ValidFrom = r.ValidFrom.ToUniversalTime(),
                ValidTo = r.ValidTo?.ToUniversalTime(),
                Rules = r.Rules,
                Version =
                    await _repo.GetNextCancellationVersionAsync(
                        propertyId,
                        ct),
                FreeCancellationHours =
                    r.FreeCancellationHours,
                CancellationFeePercentage =
                    r.CancellationFeePercentage,
                FixedCancellationFee =
                    r.FixedCancellationFee,
                IsNonRefundable =
                    r.IsNonRefundable,
                CutoffHours =
                    r.CutoffHours,
                CreatedAt =
                    DateTimeOffset.UtcNow,
                CreatedBy =
                    ActorId()
            };

            old.ValidTo =
                DateTimeOffset.UtcNow;

            old.Status =
                PolicyStatus.Inactive;

            await _repo.AddAsync(policy, ct);

            await _repo.AddAuditLogAsync(
                Audit(
                    "CancellationPolicy",
                    "Update",
                    policy.Id.ToString(),
                    old,
                    policy,
                    r.Reason,
                    propertyId),
                ct);

            await _repo.SaveChangesAsync(ct);

            return Created(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<CancellationPolicyResponse>>
            SetCancellationPolicyStatusAsync(
                int propertyId,
                int policyId,
                bool active,
                CancellationToken ct = default)
        {
            return await SetCancellation(
                propertyId,
                policyId,
                active,
                ct);
        }

        // ============================================================
        // Deposit Policies
        // ============================================================

        public async Task<
            ResponseStatus<IReadOnlyList<DepositPolicyResponse>>>
            GetDepositPoliciesAsync(
                int propertyId,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    false,
                    ct);

            if (!auth.Success)
            {
                return Fail<
                    IReadOnlyList<DepositPolicyResponse>>(auth);
            }

            var policies =
                await _repo.GetDepositPoliciesAsync(
                    propertyId,
                    ct);

            return Ok<
                IReadOnlyList<DepositPolicyResponse>>(
                policies
                    .Select(ToResponse)
                    .ToArray());
        }

        public async Task<
            ResponseStatus<DepositPolicyResponse>>
            GetDepositPolicyAsync(
                int propertyId,
                int policyId,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    false,
                    ct);

            if (!auth.Success)
            {
                return Fail<DepositPolicyResponse>(auth);
            }

            var policy =
                await _repo.GetDepositPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (policy is null)
            {
                return Fail<DepositPolicyResponse>(
                    "Deposit policy not found.",
                    404);
            }

            return Ok(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<DepositPolicyResponse>>
            CreateDepositPolicyAsync(
                int propertyId,
                CreateDepositPolicyRequest r,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<DepositPolicyResponse>(auth);
            }

            var errors =
                ValidateDates(
                    r.ValidFrom,
                    r.ValidTo);

            if (
                r.Amount < 0
                || r.Percentage < 0
                || (
                    r.Type == DepositType.Percentage
                    && r.Percentage > 100)
                || errors.Count > 0
                || (
                    r.Type == DepositType.Percentage
                    && r.Amount != 0)
                || (
                    r.Type == DepositType.FixedAmount
                    && r.Percentage != 0))
            {
                return Fail<DepositPolicyResponse>(
                    "Invalid deposit policy.",
                    400,
                    errors);
            }

            var policy = new DepositPolicy
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Type = r.Type,
                Amount = r.Amount,
                Percentage = r.Percentage,
                ValidFrom =
                    r.ValidFrom.ToUniversalTime(),
                ValidTo =
                    r.ValidTo?.ToUniversalTime(),
                Version =
                    await _repo.GetNextDepositVersionAsync(
                        propertyId,
                        ct),
                CreatedAt =
                    DateTimeOffset.UtcNow,
                CreatedBy =
                    ActorId()
            };

            await _repo.AddAsync(policy, ct);

            await _repo.AddAuditLogAsync(
                Audit(
                    "DepositPolicy",
                    "Create",
                    policy.Id.ToString(),
                    null,
                    policy,
                    null,
                    propertyId),
                ct);

            await _repo.SaveChangesAsync(ct);

            return Created(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<DepositPolicyResponse>>
            UpdateDepositPolicyAsync(
                int propertyId,
                int policyId,
                UpdateDepositPolicyRequest r,
                CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<DepositPolicyResponse>(auth);
            }

            var old =
                await _repo.GetDepositPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (old is null)
            {
                return Fail<DepositPolicyResponse>(
                    "Deposit policy not found.",
                    404);
            }

            var errors =
                ValidateDates(
                    r.ValidFrom,
                    r.ValidTo);

            if (
                r.Amount < 0
                || r.Percentage < 0
                || (
                    r.Type == DepositType.Percentage
                    && (
                        r.Percentage > 100
                        || r.Amount != 0))
                || (
                    r.Type == DepositType.FixedAmount
                    && r.Percentage != 0)
                || errors.Count > 0)
            {
                return Fail<DepositPolicyResponse>(
                    "Invalid deposit policy.",
                    400,
                    errors);
            }

            var policy = new DepositPolicy
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Type = r.Type,
                Amount = r.Amount,
                Percentage = r.Percentage,
                ValidFrom =
                    r.ValidFrom.ToUniversalTime(),
                ValidTo =
                    r.ValidTo?.ToUniversalTime(),
                Version =
                    await _repo.GetNextDepositVersionAsync(
                        propertyId,
                        ct),
                CreatedAt =
                    DateTimeOffset.UtcNow,
                CreatedBy =
                    ActorId()
            };

            old.ValidTo =
                DateTimeOffset.UtcNow;

            old.IsActive = false;

            await _repo.AddAsync(policy, ct);

            await _repo.AddAuditLogAsync(
                Audit(
                    "DepositPolicy",
                    "Update",
                    policy.Id.ToString(),
                    old,
                    policy,
                    r.Reason,
                    propertyId),
                ct);

            await _repo.SaveChangesAsync(ct);

            return Created(ToResponse(policy));
        }

        public async Task<
            ResponseStatus<DepositPolicyResponse>>
            SetDepositPolicyStatusAsync(
                int propertyId,
                int policyId,
                bool active,
                CancellationToken ct = default)
        {
            return await SetDeposit(
                propertyId,
                policyId,
                active,
                ct);
        }

        // ============================================================
        // Private Methods
        // ============================================================

        private async Task<ResponseStatus<PropertyResponse>>
            SetPropertyStatus(
                int id,
                PropertyStatus status,
                string? reason,
                CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(
                    id,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<PropertyResponse>(auth);
            }

            var property =
                await _repo.GetTrackedByIdAsync(id, ct);

            if (property is null)
            {
                return Fail<PropertyResponse>(
                    "Property not found.",
                    404);
            }

            if (
                status == PropertyStatus.Inactive
                && await _repo.HasActiveReservationsAsync(
                    id,
                    ct)
                && !IsGlobalAdmin())
            {
                return Fail<PropertyResponse>(
                    "Active reservations require Manager or Admin authorization before deactivation.",
                    403);
            }

            var oldStatus =
                property.Status;

            property.Status =
                status;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            property.UpdatedBy =
                ActorId();

            await _repo.AddAuditLogAsync(
                Audit(
                    "Property",
                    status.ToString(),
                    id.ToString(),
                    new
                    {
                        Status = oldStatus
                    },
                    new
                    {
                        Status = status
                    },
                    reason),
                ct);

            if (!await Save(ct))
            {
                return Fail<PropertyResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(property));
        }

        private async Task<ResponseStatus<TaxResponse>>
            SetTax(
                int propertyId,
                int taxId,
                bool active,
                CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

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
                ActorId();

            await _repo.AddAuditLogAsync(
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

            if (!await Save(ct))
            {
                return Fail<TaxResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(tax));
        }

        private async Task<
            ResponseStatus<CancellationPolicyResponse>>
            SetCancellation(
                int propertyId,
                int policyId,
                bool active,
                CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<CancellationPolicyResponse>(auth);
            }

            var policy =
                await _repo.GetTrackedCancellationPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (policy is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Cancellation policy not found.",
                    404);
            }

            policy.Status =
                active
                    ? PolicyStatus.Active
                    : PolicyStatus.Inactive;

            await _repo.AddAuditLogAsync(
                Audit(
                    "CancellationPolicy",
                    active
                        ? "Activate"
                        : "Deactivate",
                    policyId.ToString(),
                    null,
                    new
                    {
                        policy.Status
                    },
                    null,
                    propertyId),
                ct);

            if (!await Save(ct))
            {
                return Fail<CancellationPolicyResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(policy));
        }

        private async Task<
            ResponseStatus<DepositPolicyResponse>>
            SetDeposit(
                int propertyId,
                int policyId,
                bool active,
                CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(
                    propertyId,
                    true,
                    ct);

            if (!auth.Success)
            {
                return Fail<DepositPolicyResponse>(auth);
            }

            var policy =
                await _repo.GetTrackedDepositPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (policy is null)
            {
                return Fail<DepositPolicyResponse>(
                    "Deposit policy not found.",
                    404);
            }

            policy.IsActive = active;

            await _repo.AddAuditLogAsync(
                Audit(
                    "DepositPolicy",
                    active
                        ? "Activate"
                        : "Deactivate",
                    policyId.ToString(),
                    null,
                    new
                    {
                        policy.IsActive
                    },
                    null,
                    propertyId),
                ct);

            if (!await Save(ct))
            {
                return Fail<DepositPolicyResponse>(
                    "Concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(policy));
        }

      

        private async Task<ResponseStatus<bool>> AuthorizeAsync(
       string actorId,
       int? propertyId,
       CancellationToken cancellationToken = default)
        {
            var actor = await repository.GetByIdAsync(actorId, cancellationToken);

            if (actor is null)
            {
                return new ResponseStatus<bool>(
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

            // Admin has global access
            if (isAdmin)
            {
                return new ResponseStatus<bool>(
                    data: true,
                    statusCode: 200);
            }

            var isManager = roles.Any(role =>
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
            if (!actor.PropertyId.HasValue)
            {
                return new ResponseStatus<bool>(
                    message: "Manager is not assigned to a property.",
                    statusCode: 403);
            }

            // Manager can access only his property
            if (!propertyId.HasValue ||
                actor.PropertyId.Value != propertyId.Value)
            {
                return new ResponseStatus<bool>(
                    message: "You are not authorized to access this property.",
                    statusCode: 403);
            }

            return new ResponseStatus<bool>(
                data: true,
                statusCode: 200);
        }
        private AddAuditLogDto Audit(
            string entity,
            string action,
            string? id,
            object? oldValue,
            object? newValue,
            string? reason = null,
            int? propertyId = null)
        {
            return new AddAuditLogDto
            {
                userid =
                    ActorId() ?? "system",

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
                    _http.HttpContext?.TraceIdentifier
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

        private static bool CheckVersion(
            string? expected,
            byte[] actual)
        {
            return
                string.IsNullOrWhiteSpace(expected)
                ||
                Convert.ToBase64String(
                    actual ?? Array.Empty<byte>())
                == expected;
        }
        private static bool ValidCheckInCheckOutTime(
    TimeSpan checkInTime,
    TimeSpan checkOutTime)
        {
            return checkInTime != checkOutTime;
        }
        private static List<string> ValidateProperty(
            CreatePropertyRequest r)
        {
            var errors =
                new List<string>();

            if (
                string.IsNullOrWhiteSpace(r.Name)
                ||
                r.Name.Trim().Length < 2)
            {
                errors.Add(
                    "Name must contain at least 2 characters.");
            }

            if (
                string.IsNullOrWhiteSpace(r.Code)
                ||
                r.Code.Trim().Length < 2)
            {
                errors.Add(
                    "Code must contain at least 2 characters.");
            }

            if (!ValidZone(r.TimeZone))
            {
                errors.Add(
                    "TimeZone is invalid.");
            }

            if (
                r.Currency.Trim().Length
                is < 3 or > 10)
            {
                errors.Add(
                    "Currency must be between 3 and 10 characters.");
            }
            if (!ValidCheckInCheckOutTime(
           r.CheckInTime,
           r.CheckOutTime))
            {
                errors.Add(
                    "Check-in time must be different from check-out time.");
            }
            return errors;
        }

        private static List<string> ValidateDates(
            DateTimeOffset from,
            DateTimeOffset? to)
        {
            if (
                to.HasValue
                &&
                to.Value < from)
            {
                return new List<string>
                {
                    "ValidTo cannot precede ValidFrom."
                };
            }

            return new List<string>();
        }

        private static List<string> ValidatePolicy(
            DateTimeOffset from,
            DateTimeOffset? to,
            decimal fee,
            int free,
            int cutoff,
            string rules)
        {
            var errors =
                ValidateDates(from, to);

            if (fee is < 0 or > 100)
            {
                errors.Add(
                    "Cancellation fee percentage must be between 0 and 100.");
            }

            if (free < 0 || cutoff < 0)
            {
                errors.Add(
                    "Policy hours cannot be negative.");
            }

            try
            {
                JsonDocument.Parse(
                    string.IsNullOrWhiteSpace(rules)
                        ? "{}"
                        : rules);
            }
            catch
            {
                errors.Add(
                    "Rules must be valid JSON.");
            }

            return errors;
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

        private static string R(byte[] bytes)
        {
            return Convert.ToBase64String(
                bytes ?? Array.Empty<byte>());
        }

        private static PropertyResponse ToResponse(
            Property property)
        {
            return new PropertyResponse
            {
                Id = property.ID,
                Name = property.Name,
                Code = property.Code,
                Description = property.Description,
                Status = property.Status,

                Settings =
                    property.Settings is null
                        ? null
                        : ToResponse(property.Settings),

                CreatedAt =
                    property.CreatedAt,

                CreatedBy =
                    property.CreatedBy,

                UpdatedAt =
                    property.UpdatedAt,

                UpdatedBy =
                    property.UpdatedBy,

                RowVersion =
                    R(property.RowVersion),

                ActiveTaxes =
                    property.Taxes?
                        .Count(x => x.IsActive)
                    ?? 0,

                ActiveCancellationPolicies =
                    property.CancellationPolicies?
                        .Count(
                            x => x.Status ==
                                 PolicyStatus.Active)
                    ?? 0,

                ActiveDepositPolicies =
                    property.DepositPolicies?
                        .Count(x => x.IsActive)
                    ?? 0
            };
        }

        private static PropertySettingsResponse ToResponse(
            PropertySettings settings)
        {
            return new PropertySettingsResponse
            {
                PropertyId =
                    settings.PropertyId,

                CheckInTime =
                    settings.CheckInTime,

                CheckOutTime =
                    settings.CheckOutTime,

                AllowEarlyCheckIn =
                    settings.AllowEarlyCheckIn,

                HotelDayClosingTime =
                    settings.HotelDayClosingTime,

                DefaultCurrency =
                    settings.DefaultCurrency,

                TimeZone =
                    settings.TimeZone,

                RequireDepositForReservation =
                    settings.RequireDepositForReservation,

                RequireFullPaymentBeforeCheckOut =
                    settings.RequireFullPaymentBeforeCheckOut,

                AllowOverpayment =
                    settings.AllowOverpayment,

                RequireInspectionBeforeAvailable =
                    settings.RequireInspectionBeforeAvailable,

                RowVersion =
                    R(settings.RowVersion)
            };
        }

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
                RowVersion = R(tax.RowVersion)
            };
        }

        private static CancellationPolicyResponse ToResponse(
            CancellationPolicy policy)
        {
            return new CancellationPolicyResponse
            {
                Id = policy.Id,
                PropertyId = policy.PropertyId,
                Name = policy.Name,
                Description = policy.Description,
                ValidFrom = policy.ValidFrom,
                ValidTo = policy.ValidTo,
                Status = policy.Status,
                Rules = policy.Rules,
                Version = policy.Version,
                FreeCancellationHours =
                    policy.FreeCancellationHours,
                CancellationFeePercentage =
                    policy.CancellationFeePercentage,
                FixedCancellationFee =
                    policy.FixedCancellationFee,
                IsNonRefundable =
                    policy.IsNonRefundable,
                CutoffHours =
                    policy.CutoffHours,
                RowVersion =
                    R(policy.RowVersion)
            };
        }

        private static DepositPolicyResponse ToResponse(
            DepositPolicy policy)
        {
            return new DepositPolicyResponse
            {
                Id = policy.Id,
                PropertyId = policy.PropertyId,
                Name = policy.Name,
                Type = policy.Type,
                Amount = policy.Amount,
                Percentage = policy.Percentage,
                ValidFrom = policy.ValidFrom,
                ValidTo = policy.ValidTo,
                IsActive = policy.IsActive,
                Version = policy.Version,
                RowVersion = R(policy.RowVersion)
            };
        }
    }
}

