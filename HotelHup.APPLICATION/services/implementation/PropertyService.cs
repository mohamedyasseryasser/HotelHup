using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.APPLICATION.DTO.property.propertydto;
using HotelHup.APPLICATION.DTO.property.propertysettingdto;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Property = HotelHup.CORE.Entities.Property;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class PropertyService : IPropertyService
    {
        private readonly IUserRepository repository;
        public readonly IPropertyRepository _repo;
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
            //check actor is admin or PropertyManager
            var auth = await AuthorizeAsync(CurrentUserLogin.Id, null, ct);

            if (!auth.Success)
            {
                return Fail<PropertyListResponse>(auth);
            }
            if (r==null)
            {
                
                return Fail<PropertyListResponse>(
                    message: "request is null",
                    statusCode: 400
                    );
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
            User currentUserLogin,
            int id,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(
                currentUserLogin.Id,
                id,
                ct);

            if (!auth.Success)
            {
                return Fail<PropertyResponse>(
                    auth.Message,
                    auth.StatusCode);
            }
            var property = await _repo.GetByIdAsync(id,ct);
            if (property==null)
            {
                return 
                    new 
                    ResponseStatus<PropertyResponse>
                    (message:"this property is not found",
                    statusCode:400);
            }
            return Ok(ToResponse(property));
        }

        public async Task<ResponseStatus<PropertyResponse>> CreateAsync(
           User CurrentUserLogin, CreatePropertyRequest r,
            CancellationToken ct = default)
        {
            //check currentuserlogin is admin 
            var result = await IsAdminAsync(CurrentUserLogin.Id, ct);
            if (!result.Success)
            {
                return new ResponseStatus<PropertyResponse>(
                    message: result.Message,
                    statusCode: result.StatusCode,errors:result.Errors);
            }

            if (result.Data is null || !result.Data.IsAdmin)
            {
                return new ResponseStatus<PropertyResponse>(
                    message: "Admin access required.",
                    statusCode: 403,errors:result.Errors);
            }
            if (r is null)
            {
                return Fail<PropertyResponse>(
                    "Request body is required.",
                    400);
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
           User CurrentUserLogin ,string ifmatch,int id,
            UpdatePropertyRequest r,
            CancellationToken ct = default)
        {
            //check currentuserlogin is admin or manager
            var auth = await AuthorizeAsync(CurrentUserLogin.Id,id,ct);
             if (!auth.Success)
             {
                return Fail<PropertyResponse>(auth);
            }
            if (r is null)
            {
                return Fail<PropertyResponse>(
                    "Request body is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifmatch))
            {
                return Fail<PropertyResponse>(
                    "If-Match header is required.",
                    428);
            }

            if (string.IsNullOrWhiteSpace(r.Reason))
            {
                return Fail<PropertyResponse>(
                    "reason is required.",
                    400);
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
            if (!CheckRowVersion(
                    ifmatch,
                    property.RowVersion))
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
          string expected, User actor, int id,
            CancellationToken ct = default)
        {

            return await SetPropertyStatus(
            expected,  actor,  id,
                PropertyStatus.Active,
                null,
                ct);
        }

        public async Task<ResponseStatus<PropertyResponse>> DeactivateAsync(
          string excepted, User actor ,int id,
            DeactivatePropertyRequest r,
            CancellationToken ct = default)
        {
            return await SetPropertyStatus(
          excepted,     actor, id,
                PropertyStatus.Inactive,
                r?.Reason,
                ct);
        }

        public async Task<ResponseStatus<PropertySettingsResponse>>
            GetSettingsAsync(
                User CurrentUserLogin,int id,
                CancellationToken ct = default)
        {
            var auth = await AuthorizePropertyAccessAsync(
                            CurrentUserLogin.Id,
                            id,
                            ct);

            if (!auth.Success)
            {
                return Fail<PropertySettingsResponse>(
                    auth.Message,
                    auth.StatusCode);
            }
            if (auth.Data is null||auth.Data.Settings is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Settings not found.",
                    404);
            }
            var settings = auth.Data.Settings;
            return Ok(ToResponse(settings));
        }

        public async Task<ResponseStatus<PropertySettingsResponse>>
            UpdateSettingsAsync(
               User actor, int id,string ifmatch,
                UpdatePropertySettingsRequest r,
                CancellationToken ct = default)
        {
            if (r is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Request body is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifmatch))
            {
                return Fail<PropertySettingsResponse>(
                    "If-Match header is required.",
                    428);
            }
            //check property
            var property =
               await _repo.GetTrackedByIdAsync(id, ct);

            if (property is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Property not found.",
                    404);
            }
            if (property.Status == PropertyStatus.Inactive)
            {
                return Fail<PropertySettingsResponse>(
                   "Property not active.",
                   404);
            }
            //check admin or manager
            var auth = await AuthorizeAsync(actor.Id,property.ID,ct);

            if (!auth.Success)
            {
                return Fail<PropertySettingsResponse>(
                    auth.Message,
                    auth.StatusCode);
            }
            var settings =
                await _repo.GetTrackedSettingsAsync(id, ct);

            if (settings is null)
            {
                return Fail<PropertySettingsResponse>(
                    "Settings not found.",
                    404);
            }

            if (!CheckRowVersion(
                    ifmatch,
                    settings.RowVersion))
            {
                return Fail<PropertySettingsResponse>(
                    "Concurrency conflict.",
                    409);
            }
            var oldValues = new
            {
                CheckInTime =
            settings.CheckInTime,

                CheckOutTime =
            settings.CheckOutTime,

                NightAuditCutoffTime =
            settings.HotelDayClosingTime,

                AllowEarlyCheckIn =
            settings.AllowEarlyCheckIn,

                Currency =
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
            settings.RequireInspectionBeforeAvailable
            };
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
              //  property.Currency = currency;
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
                actor.Id;
            var newValues = new
            {
                CheckInTime =
           settings.CheckInTime,

                CheckOutTime =
           settings.CheckOutTime,

                NightAuditCutoffTime =
           settings.HotelDayClosingTime,

                AllowEarlyCheckIn =
           settings.AllowEarlyCheckIn,

                Currency =
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
           settings.RequireInspectionBeforeAvailable
            };

            await _repo.AddAuditLogAsync(
                Audit(
                    "PropertySettings",
                    "Update",
                    id.ToString(),
                    oldValues,
                    newValues,
                    r.Reason,propertyId:property.ID,actorid:actor.Id),
                ct);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<PropertySettingsResponse>(
                    "concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(settings));
        }

       

      


        // ============================================================
        // Deposit Policies (immutable version rows)
        // ============================================================

        public async Task<ResponseStatus<IReadOnlyList<DepositPolicyResponse>>>
            GetDepositPoliciesAsync(
          User actor,  int propertyId,
            CancellationToken ct = default)
        {
            var auth =
        await AuthorizeAsync(
            actor.Id,
            propertyId,
            ct);

            if (!auth.Success)
                return Fail<IReadOnlyList<DepositPolicyResponse>>(auth);

            var policies =
                await _repo.GetDepositPoliciesAsync(
                    propertyId,
                    ct);

            return Ok<IReadOnlyList<DepositPolicyResponse>>(
                policies
                    .Select(ToResponse)
                    .ToArray());
        }

        public async Task<
          ResponseStatus<DepositPolicyResponse>>
          GetDepositPolicyAsync(
              User actor,
              int propertyId,
              int policyId,
              CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!auth.Success)
                return Fail<DepositPolicyResponse>(auth);

            var policy =
                await _repo.GetDepositPolicyAsync(
                    propertyId,
                    policyId,
                    ct);

            if (policy is null)
                return Fail<DepositPolicyResponse>(
                    "Deposit policy not found.",
                    404);

            return Ok(ToResponse(policy));
        }


        public async Task<
     ResponseStatus<DepositPolicyResponse>>
     CreateDepositPolicyAsync(
         User actor,
         int propertyId,
         CreateDepositPolicyRequest request,
         CancellationToken ct = default)
        {
            var auth =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!auth.Success)
                return Fail<DepositPolicyResponse>(auth);

            var errors =
                ValidateDeposit(request, null);

            if (errors.Count > 0)
            {
                return Fail<DepositPolicyResponse>(
                    "Invalid deposit policy.",
                    400,
                    errors);
            }

            var now =
                DateTimeOffset.UtcNow;

            var policy = new DepositPolicy
            {
                PropertyId = propertyId,
                Name = Clean(request.Name),
                Type = request.Type,
                Amount = request.Amount,
                Percentage = request.Percentage,
                ValidFrom =
                    request.ValidFrom.ToUniversalTime(),
                ValidTo =
                    request.ValidTo?.ToUniversalTime(),
                IsActive = true,
                CurrentVersion = 1,
                CreatedAt = now,
                CreatedBy = actor.Id
            };

            var version =
                NewDepositVersion(
                    policy,
                    request,
                    1,
                    now,
                    actor.Id);

            policy.Versions.Add(version);

            try
            {
                await _repo.AddDepositPolicyWithAuditAsync(
                    policy,
                    Audit(
                        "DepositPolicy",
                        "Create",
                        "pending",
                        null,
                        version,
                        null,
                        propertyId,
                        actor.Id),
                    ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<DepositPolicyResponse>(
                    "Concurrency conflict.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<DepositPolicyResponse>(
                    "Deposit policy could not be created.",
                    409);
            }

            return Created(ToResponse(policy));
        }


        public async Task<ResponseStatus<DepositPolicyResponse>>
          UpdateDepositPolicyAsync(
              User actor,
              int propertyId,
              int policyId,
              UpdateDepositPolicyRequest request,
              string ifMatch,
              CancellationToken ct = default)
        {
            if (actor is null)
            {
                return Fail<DepositPolicyResponse>(
                    "Authenticated user is required.",
                    401);
            }

            if (request is null)
            {
                return Fail<DepositPolicyResponse>(
                    "Request body is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<DepositPolicyResponse>(
                    "If-Match header is required.",
                    428);
            }

            var authorization =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<DepositPolicyResponse>(
                    authorization.Message,
                    authorization.StatusCode);
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

            if (!CheckRowVersion(
                    ifMatch,
                    policy.RowVersion))
            {
                return Fail<DepositPolicyResponse>(
                    "Concurrency conflict. The deposit policy was modified by another user.",
                    409);
            }

            var previousVersion =
                policy.Versions
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

            if (previousVersion is null)
            {
                return Fail<DepositPolicyResponse>(
                    "The deposit policy has no previous version.",
                    409);
            }

            var validationErrors =
                ValidateDepositUpdate(
                    request,
                    previousVersion);

            if (validationErrors.Count > 0)
            {
                return Fail<DepositPolicyResponse>(
                    "Invalid deposit policy.",
                    400,
                    validationErrors);
            }

            var now =
                DateTimeOffset.UtcNow;

            var newValidFrom =
                request.ValidFrom.ToUniversalTime();

            var newValidTo =
                request.ValidTo?.ToUniversalTime();

            /*
             * نأخذ snapshot قبل تعديل الإصدار القديم.
             */
            var oldVersionSnapshot = new
            {
                previousVersion.Id,
                previousVersion.DepositId,
                previousVersion.Version,
                previousVersion.Name,
                previousVersion.Type,
                previousVersion.Amount,
                previousVersion.Percentage,
                previousVersion.ValidFrom,
                previousVersion.ValidTo,
                previousVersion.IsActive
            };

            /*
             * إغلاق الإصدار السابق عند بداية الإصدار الجديد.
             */
            if (!previousVersion.ValidTo.HasValue ||
                previousVersion.ValidTo.Value > newValidFrom)
            {
                previousVersion.ValidTo =
                    newValidFrom;
            }

            previousVersion.IsActive =
                false;

            var nextVersionNumber =
                policy.CurrentVersion + 1;

            var newVersion =
                new DepositPolicyVersion
                {
                    Deposit =
                        policy,

                    Version =
                        nextVersionNumber,

                    Name =
                        Clean(request.Name),

                    Type =
                        request.Type,

                    Amount =
                        decimal.Round(
                            request.Amount,
                            2,
                            MidpointRounding.AwayFromZero),

                    Percentage =
                        decimal.Round(
                            request.Percentage,
                            2,
                            MidpointRounding.AwayFromZero),

                    ValidFrom =
                        newValidFrom,

                    ValidTo =
                        newValidTo,

                    IsActive =
                        true,

                    CreatedAt =
                        now,

                    CreatedBy =
                        actor.Id
                };

            /*
             * نحافظ على الـ root Name كما هو.
             * الإصدار الجديد يحتوي على الـ Name الجديد.
             *
             * يتم تحديث بيانات الـ current version فقط.
             */
            policy.Type =
                request.Type;

            policy.Amount =
                request.Amount;

            policy.Percentage =
                request.Percentage;

            policy.ValidFrom =
                newValidFrom;

            policy.ValidTo =
                newValidTo;

            policy.CurrentVersion =
                nextVersionNumber;

            policy.IsActive =
                true;

            policy.UpdatedAt =
                now;

            policy.UpdatedBy =
                actor.Id;

            policy.Versions.Add(newVersion);

            await _repo.AddAuditLogAsync(
                Audit(
                    entity: "DepositPolicy",
                    action: "DepositPolicyVersionCreated",
                    id: policy.Id.ToString(),
                    oldValue: oldVersionSnapshot,
                    newValue: new
                    {
                        newVersion.Id,
                        newVersion.DepositId,
                        newVersion.Version,
                        newVersion.Name,
                        newVersion.Type,
                        newVersion.Amount,
                        newVersion.Percentage,
                        newVersion.ValidFrom,
                        newVersion.ValidTo,
                        newVersion.IsActive
                    },
                    reason: request.Reason,
                    propertyId: propertyId),
                ct);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<DepositPolicyResponse>(
                    "Concurrency conflict. The deposit policy was modified by another user.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<DepositPolicyResponse>(
                    "The deposit policy version could not be created.",
                    409);
            }

            return Ok(
                ToResponse(policy));
        }


        public async Task<ResponseStatus<DepositPolicyResponse>>
            SetDepositPolicyStatusAsync(int propertyId,
            int policyId,
            bool active,
            CancellationToken ct = default)
            => await SetDeposit(propertyId, policyId, active, ct);




        // ============================================================
        // Private Methods
        // ============================================================

        private async Task<ResponseStatus<PropertyResponse>>
            SetPropertyStatus(string expected,
            User actor,    int id,
                PropertyStatus status,
                string? reason,
                CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(actor.Id, id, ct);

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
            var requeststate = status;
            if (requeststate==property.Status)
            {
                return Ok<PropertyResponse>(ToResponse(property));
            }
            var isadmin = await IsAdminAsync(actor.Id,ct);
            if (
                status == PropertyStatus.Inactive
                && await _repo.HasActiveReservationsAsync(
                    id,
                    ct)
                &&( isadmin.Data!=null&&!isadmin.Data.IsAdmin))
            {
                return Fail<PropertyResponse>(
                    "Active reservations require Manager or Admin authorization before deactivation.",
                    403);
            }
            // Validate expected RowVersion
            if (property.Settings == null)
            {
                return Fail<PropertyResponse>(
                    "Property settings not found. please add property setting",
                    404);
            }
            if (!CheckRowVersion(expected, property.RowVersion))
            {
                return Fail<PropertyResponse>(
                    "The property was modified by another user. Please refresh and try again.",
                    409);
            }


            var oldStatus =
                property.Status;

            property.Status =
                status;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            property.UpdatedBy =
                actor.Id;

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

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<PropertyResponse>(
                    "The property was modified by another user. Please refresh and try again.",
                    409);
            }

            return Ok(ToResponse(property));
        }

        


        private static List<string> ValidateDeposit(
      CreateDepositPolicyRequest request,
      DepositPolicy? existing)
        {
            var errors = new List<string>();

            var newValidFrom =
                request.ValidFrom.ToUniversalTime();

            var newValidTo =
                request.ValidTo?.ToUniversalTime();

            // التحقق من أن ValidTo أكبر من ValidFrom
            if (newValidTo.HasValue &&
                newValidTo.Value <= newValidFrom)
            {
                errors.Add(
                    "ValidTo must be greater than ValidFrom.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Name is required.");
            }

            if (request.Amount < 0 ||
                request.Percentage < 0)
            {
                errors.Add(
                    "Amount and percentage cannot be negative.");
            }

            if (request.Type == DepositType.Percentage &&
                (request.Percentage > 100 ||
                 request.Amount != 0))
            {
                errors.Add(
                    "Percentage deposits require amount 0 and percentage between 0 and 100.");
            }

            if (request.Type == DepositType.FixedAmount &&
                request.Percentage != 0)
            {
                errors.Add(
                    "Fixed amount deposits require percentage 0.");
            }

            /*
             * في حالة إنشاء DepositPolicy جديدة:
             * existing ستكون null، وبالتالي لا يوجد إصدار سابق
             * ونكتفي بالـ validation العادي فقط.
             */
            if (existing is null)
            {
                return errors;
            }

            var previousVersion =
                existing.Versions
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

            /*
             * لو الـ Policy موجودة لكن ليس لديها Versions،
             * لا يوجد إصدار نقارن به.
             */
            if (previousVersion is null)
            {
                return errors;
            }

            var versionDateErrors =
                ValidateNewVersionDates(
                    newValidFrom: newValidFrom,
                    newValidTo: newValidTo,
                    previousValidFrom: previousVersion.ValidFrom,
                    previousValidTo: previousVersion.ValidTo,
                    policyName: "deposit policy");

            errors.AddRange(versionDateErrors);

            return errors;
        }
        private static List<string> ValidateNewVersionDates(
    DateTimeOffset newValidFrom,
    DateTimeOffset? newValidTo,
    DateTimeOffset previousValidFrom,
    DateTimeOffset? previousValidTo,
    string policyName)
        {
            var errors = new List<string>();

            var newFrom =
                newValidFrom.ToUniversalTime();

            var newTo =
                newValidTo?.ToUniversalTime();

            var previousFrom =
                previousValidFrom.ToUniversalTime();

            var previousTo =
                previousValidTo?.ToUniversalTime();

            // بداية الإصدار الجديد يجب أن تكون بعد بداية الإصدار السابق
            if (newFrom <= previousFrom)
            {
                errors.Add(
                    $"New {policyName} version ValidFrom must be greater than the previous version ValidFrom.");
            }

            /*
             * إذا كان للإصدار السابق ValidTo،
             * فلا يمكن أن يبدأ الإصدار الجديد قبله.
             */
            if (previousTo.HasValue &&
                newFrom < previousTo.Value)
            {
                errors.Add(
                    $"New {policyName} version overlaps the previous version.");
            }

            // ValidTo للإصدار الجديد يجب أن تكون بعد ValidFrom
            if (newTo.HasValue &&
                newTo.Value <= newFrom)
            {
                errors.Add(
                    $"New {policyName} version ValidTo must be greater than ValidFrom.");
            }

            return errors;
        }

        private static List<string>
    ValidateDepositUpdate(
        UpdateDepositPolicyRequest request,
        DepositPolicyVersion previousVersion)
        {
            var errors =
                new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add(
                    "Name is required.");
            }
            else if (request.Name.Trim().Length > 150)
            {
                errors.Add(
                    "Name cannot exceed 150 characters.");
            }

            if (request.Amount < 0)
            {
                errors.Add(
                    "Amount cannot be negative.");
            }

            if (request.Percentage < 0)
            {
                errors.Add(
                    "Percentage cannot be negative.");
            }

            if (request.Type == DepositType.Percentage)
            {
                if (request.Percentage > 100)
                {
                    errors.Add(
                        "Percentage must be between 0 and 100.");
                }

                if (request.Amount != 0)
                {
                    errors.Add(
                        "Percentage deposits require Amount to be zero.");
                }
            }

            if (request.Type == DepositType.FixedAmount &&
                request.Percentage != 0)
            {
                errors.Add(
                    "Fixed amount deposits require Percentage to be zero.");
            }

            if (request.ValidTo.HasValue &&
                request.ValidTo.Value <= request.ValidFrom)
            {
                errors.Add(
                    "ValidTo must be greater than ValidFrom.");
            }

            errors.AddRange(
                ValidateNewVersionDates(
                    newValidFrom: request.ValidFrom,
                    newValidTo: request.ValidTo,
                    previousValidFrom: previousVersion.ValidFrom,
                    previousValidTo: previousVersion.ValidTo,
                    policyName: "deposit policy"));

            if (!string.IsNullOrWhiteSpace(request.Reason) &&
                request.Reason.Length > 500)
            {
                errors.Add(
                    "Reason cannot exceed 500 characters.");
            }

            return errors
                .Distinct()
                .ToList();
        }

        private static DepositPolicyVersion
            NewDepositVersion(DepositPolicy policy, CreateDepositPolicyRequest r, int version, DateTimeOffset now) => new()
        {
            Deposit = policy,
            Version = version,
            Name = Clean(r.Name),
            Type = r.Type,
            Amount = r.Amount,
            Percentage = r.Percentage,
            ValidFrom = r.ValidFrom.ToUniversalTime(),
            ValidTo = r.ValidTo?.ToUniversalTime(),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = null
        };
        private async Task<ResponseStatus<TaxResponse>>
            SetTax(
               User actor, int propertyId,
                int taxId,
                bool active,
                CancellationToken ct)
        {
            var auth = await AuthorizeAsync(actor.Id,propertyId,ct);

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
            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<TaxResponse>(
                    "concurrency conflict.",
                    409);
            }

            return Ok(ToResponse(tax));
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
            var currentVersion = policy.Versions.OrderByDescending(x => x.Version).FirstOrDefault();
            if (currentVersion is not null) currentVersion.IsActive = active;

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
                    ( data: true,
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
        private AddAuditLogDto Audit(
            string entity,
            string action,
            string? id,
            object? oldValue,
            object? newValue,
            string? reason = null,
            int? propertyId = null,string? actorid=null)
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
        private static bool IsValidIsoCurrency(
 string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                return false;
            }

            var value =
                currency.Trim().ToUpperInvariant();

            return value.Length == 3 &&
                   value.All(char.IsLetter);
        }
        private static List<string> ValidatePropertyTimes(
    TimeSpan checkIn,
    TimeSpan checkOut
    )
        {
            var errors = new List<string>();

            if (checkIn < TimeSpan.Zero ||
                checkIn >= TimeSpan.FromDays(1))
            {
                errors.Add(
                    "DefaultCheckInTime must be between 00:00 and 23:59:59.");
            }

            if (checkOut < TimeSpan.Zero ||
                checkOut >= TimeSpan.FromDays(1))
            {
                errors.Add(
                    "DefaultCheckOutTime must be between 00:00 and 23:59:59.");
            }
            if (checkIn == checkOut)
            {
                errors.Add(
                    "Check-in and check-out times cannot be equal.");
            }

            return errors;
        }
        private static List<string> ValidateCreateProperty(
    CreatePropertyRequest request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Name is required.");
            }

           

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add("Code is required.");
            }

            if (!IsValidIsoCurrency(
                    request.Settings.Currency))
            {
                errors.Add(
                    "Currency must be a valid ISO 4217 three-letter code.");
            }

            if (!ValidZone(
                    request.Settings.TimeZone))
            {
                errors.Add(
                    "TimeZone must be a valid IANA time zone.");
            }

            errors.AddRange(
                ValidatePropertyTimes(
                    request.Settings.CheckInTime,
                    request.Settings.CheckOutTime));

            return errors;
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
        private static List<string>
    ValidateCancellationPolicyStatusRequest(
        ChangeCancellationPolicyStatusRequest request,
        bool activating)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(
                    request.ExpectedRowVersion))
            {
                errors.Add(
                    "ExpectedRowVersion is required.");
            }

            if (!activating)
            {
                if (!request.Confirm)
                {
                    errors.Add(
                        "Confirm must be true before deactivating a cancellation policy.");
                }

                if (string.IsNullOrWhiteSpace(
                        request.Reason))
                {
                    errors.Add(
                        "Reason is required when deactivating a cancellation policy.");
                }
                else if (request.Reason.Trim().Length < 2)
                {
                    errors.Add(
                        "Reason must contain at least 2 characters.");
                }
            }

            return errors;
        }

        private bool ValidCheckInCheckOutTime(
    TimeSpan checkInTime,
    TimeSpan checkOutTime)
        {
            return checkInTime != checkOutTime;
        }
        private   List<string> ValidateProperty(
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

        private   List<string> ValidateDates(
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

        private   async Task<List<string>> ValidatePolicy(
            DateTimeOffset from,
            DateTimeOffset? to,
            decimal fee,
            int free,
            int cutoff,
            string rules,string name,int propertyid,CancellationToken ct)
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
            if (to<=from) 
            {
                errors.Add("validfrom must be less than validto");
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
        private async Task<ResponseStatus<Property>> AuthorizePropertyAccessAsync(
            string actorId,
            int propertyId,
            CancellationToken ct)
        {
            // Check actor authorization
            var auth = await IsAdminAsync(actorId, ct);

            if (!auth.Success)
            {
                return new ResponseStatus<Property>(
                    message: auth.Message,
                    statusCode: auth.StatusCode);
            }
            // 2. Authorization data must exist
            if (auth.Data is null)
            {
                return new ResponseStatus<Property>(
                    message: "Authorization data not found.",
                    statusCode: 403);
            }
            // Get property
            var property = await _repo.GetByIdAsync(propertyId, ct);

            if (property is null)
            {
                return new ResponseStatus<Property>(
                    message: "Property is not found.",
                    statusCode: 404);
            }
 
            // Admin can access all properties
            if (auth.Data?.IsAdmin == true)
            {
                return new ResponseStatus<Property>(
                    data: property,
                    statusCode: 200);
            }

            // Non-admin must belong to the property
            if (property.ID != auth.Data?.Actor.PropertyId)
            {
                return new ResponseStatus<Property>(
                    message: "You are not authorized to access this property.",
                    statusCode: 403);
            }

            return new ResponseStatus<Property>(
                data: property,
                statusCode: 200);
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
        private static DepositPolicyVersion
    NewDepositVersion(
        DepositPolicy policy,
        CreateDepositPolicyRequest request,
        int version,
        DateTimeOffset now,
        string actorId)
        {
            return new DepositPolicyVersion
            {
                Deposit = policy,
                Version = version,
                Name = Clean(request.Name),
                Type = request.Type,
                Amount = request.Amount,
                Percentage = request.Percentage,
                ValidFrom = request.ValidFrom.ToUniversalTime(),
                ValidTo = request.ValidTo?.ToUniversalTime(),
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId
            };
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

                SettingsResponse =
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

                RowVersion = settings.RowVersion
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
                Status = policy.Status,
                CurrentVersion = policy.CurrentVersion,
                RowVersion =
                    Convert.ToBase64String(
                        policy.RowVersion ?? Array.Empty<byte>()),

                Versions = policy.Versions
                    .OrderByDescending(x => x.Version)
                    .Select(x =>
                        new CancellationPolicyVersionResponse
                        {
                            Id = x.Id,
                            CancellationPolicyId =
                                x.CancellationPolicyId == 0
                                    ? policy.Id
                                    : x.CancellationPolicyId,
                            Version = x.Version,
                            ValidFrom = x.ValidFrom,
                            ValidTo = x.ValidTo,
                            Rules = x.Rules,
                            FreeCancellationHours =
                                x.FreeCancellationHours,
                            CancellationFeePercentage =
                                x.CancellationFeePercentage,
                            FixedCancellationFee =
                                x.FixedCancellationFee,
                            IsNonRefundable =
                                x.IsNonRefundable,
                            CutoffHours =
                                x.CutoffHours,
                            CreatedAt = x.CreatedAt,
                            CreatedBy = x.CreatedBy,
                            UpdatedAt = x.UpdatedAt,
                            UpdatedBy = x.UpdatedBy
                        })
                    .ToArray()
            };
        }

        private static DepositPolicyResponse
         ToResponse(DepositPolicy policy)
        {
            return new DepositPolicyResponse
            {
                Id =
                    policy.Id,

                PropertyId =
                    policy.PropertyId,

                IsActive =
                    policy.IsActive,

                CurrentVersion =
                    policy.CurrentVersion,

                RowVersion =
                    Convert.ToBase64String(
                        policy.RowVersion ?? Array.Empty<byte>()),

                Versions =
                    policy.Versions
                        .OrderByDescending(v => v.Version)
                        .Select(v => new DepositPolicyVersionResponse
                        {
                            Id =
                                v.Id,

                            DepositId =
                                v.DepositId,

                            Version =
                                v.Version,

                            Name =
                                v.Name,

                            Type =
                                v.Type,

                            Amount =
                                v.Amount,

                            Percentage =
                                v.Percentage,

                            ValidFrom =
                                v.ValidFrom,

                            ValidTo =
                                v.ValidTo,

                            IsActive =
                                v.IsActive,

                            CreatedAt =
                                v.CreatedAt,

                            CreatedBy =
                                v.CreatedBy,

                            UpdatedAt =
                                v.UpdatedAt,

                            UpdatedBy =
                                v.UpdatedBy
                        })
                        .ToList()
            };
        }
    }
}

