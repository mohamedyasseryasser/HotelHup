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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace HotelHup.APPLICATION.services.implementation
{
    public class propertydepositservicecs:IPropertyDepositService
    {
        public IHttpContextAccessor HttpContextAccessor { get; }
        public IUserRepository UserService { get; }
        public IPropertyTaxRepo PropertyTaxRepo { get; }
        public IPropertyRepository PropertyRepository { get; }
        public IPropertyDepositRepo _repo { get; }

        public propertydepositservicecs(IHttpContextAccessor httpContextAccessor,IUserRepository userService,IPropertyTaxRepo propertyTaxRepo,IPropertyRepository propertyRepository,IPropertyDepositRepo repo)
        {
            HttpContextAccessor = httpContextAccessor;
            UserService = userService;
            PropertyTaxRepo = propertyTaxRepo;
            PropertyRepository = propertyRepository;
            _repo = repo;
        }
        public async Task<ResponseStatus<IReadOnlyList<DepositPolicyResponse>>>
            GetDepositPoliciesAsync(
          User actor, int propertyId,
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

            await PropertyRepository.AddAuditLogAsync(
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
                await PropertyRepository.SaveChangesAsync(ct);
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
        SetDepositPolicyStatusAsync(
            User actor,
            int propertyId,
            int policyId,
            bool active,
            string? reason,
            string? ifMatch,
            CancellationToken ct = default)
        => await SetDeposit(
            actor,
            propertyId,
            policyId,
            active,
            reason,
            ifMatch,
            ct);



        //-----------------------------------------------
        //private method
        //-----------------------------------------------
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
            var auth = await AuthorizeAsync(actor.Id, propertyId, ct);

            if (!auth.Success)
            {
                return Fail<TaxResponse>(auth);
            }

            var tax =
                await PropertyTaxRepo.GetTrackedTaxAsync(
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

        private async Task<
       ResponseStatus<DepositPolicyResponse>>
       SetDeposit(
           User actor,
           int propertyId,
           int policyId,
           bool active,
           string? reason,
           string? ifMatch,
           CancellationToken ct)
        {
            var auth =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
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

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<DepositPolicyResponse>(
                    "If-Match header is required.",
                    428);
            }

            if (!CheckRowVersion(
                    ifMatch,
                    policy.RowVersion))
            {
                return Fail<DepositPolicyResponse>(
                    "Concurrency conflict. The deposit policy was modified by another user.",
                    409);
            }

            if (string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length < 2)
            {
                return Fail<DepositPolicyResponse>(
                    "Reason is required and must contain at least 2 characters.",
                    400);
            }

            if (reason.Trim().Length > 500)
            {
                return Fail<DepositPolicyResponse>(
                    "Reason cannot exceed 500 characters.",
                    400);
            }

            var oldSnapshot = new
            {
                policy.Id,
                policy.PropertyId,
                policy.IsActive,
                policy.CurrentVersion,
                Version = policy.Versions
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault()
            };

            policy.IsActive = active;

            var currentVersion =
                policy.Versions
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

            if (currentVersion is not null)
            {
                currentVersion.IsActive = active;
            }

            policy.UpdatedAt =
                DateTimeOffset.UtcNow;

            policy.UpdatedBy =
                actor.Id;

            var newSnapshot = new
            {
                policy.Id,
                policy.PropertyId,
                policy.IsActive,
                policy.CurrentVersion,
                Version = currentVersion
            };

            try
            {
                await _repo.SetDepositPolicyStatusWithAuditAsync(
                    policy,
                    Audit(
                        "DepositPolicy",
                        active
                            ? "Activate"
                            : "Deactivate",
                        policyId.ToString(),
                        oldSnapshot,
                        newSnapshot,
                        reason.Trim(),
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
                    "Deposit policy status could not be changed.",
                    409);
            }

            return Ok(
                ToResponse(policy));
        }


        private async Task<ResponseStatus<AuthorizationDataDto>> IsAdminAsync(
            string actorId,
            CancellationToken cancellationToken = default)
        {
            var actor = await UserService.GetByIdAsync(
                actorId,
                cancellationToken);

            if (actor is null)
            {
                return new ResponseStatus<AuthorizationDataDto>(
                    message: "User not found.",
                    statusCode: 404);
            }

            var roles = await UserService.GetRolesAsync(
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

                CorrelationId =GetCorrelationId()
             };
        }
        private string GetCorrelationId()
        {
            var httpContext = HttpContextAccessor.HttpContext;

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
        private static string Clean(
            string value)
        {
            return value.Trim();
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
