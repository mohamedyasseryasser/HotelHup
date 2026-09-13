using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
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
    public class Cancellationpolicyservice : ICancellationPolicyService
    {
        public IPropertyRepository PropertyRepository { get; }
        public IPropertyCancellationRepo _repo { get; }
        public IUserRepository repository { get; }

        public Cancellationpolicyservice(IPropertyRepository propertyRepository,IPropertyCancellationRepo repo,IUserRepository repository)
        {
            PropertyRepository = propertyRepository;
            _repo = repo;
            repository = repository;
        }
      
        public async Task<ResponseStatus<IReadOnlyList<CancellationPolicyResponse>>>
            GetCancellationPoliciesAsync(User actor, int propertyId,
            CancellationToken ct = default)
        {


            var authorization =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<IReadOnlyList<CancellationPolicyResponse>>(
                    authorization.Message,
                    authorization.StatusCode);
            }

            var policies =
                await _repo.GetCancellationPoliciesAsync(
                    propertyId,
                    ct);

            var response =
                policies
                    .Select(ToResponse)
                    .ToArray();

            return Ok<IReadOnlyList<CancellationPolicyResponse>>(
                response);
        }

        public async Task<ResponseStatus<CancellationPolicyResponse>>
            GetCancellationPolicyAsync(User actor, int propertyId,
            int policyId, CancellationToken ct = default)
        {
            var authorization =
       await AuthorizeAsync(
           actor.Id,
           propertyId,
           ct);

            if (!authorization.Success)
            {
                return Fail<CancellationPolicyResponse>(
                    authorization.Message,
                    authorization.StatusCode);
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

            return Ok(
                ToResponse(policy));
        }

        public async Task<ResponseStatus<CancellationPolicyResponse>>
        CreateCancellationPolicyAsync(
            User actor,
            int propertyId,
            CreateCancellationPolicyRequest r,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(actor.Id, propertyId, ct);

            if (!auth.Success)
            {
                return Fail<CancellationPolicyResponse>(auth);
            }

            if (r is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Request body is required.",
                    400);
            }

            var property = await PropertyRepository.GetByIdAsync(
                propertyId,
                ct);

            if (property is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Property not found.",
                    404);
            }

            if (property.Status == PropertyStatus.Inactive)
            {
                return Fail<CancellationPolicyResponse>(
                    "Cannot create a cancellation policy for an inactive property.",
                    409);
            }
            //check unique name in this property
            var normalizename = Normalize(r.Name);

            if (await _repo.CancellationPolicyNameExistsAsync(property.ID, normalizename, ct))
            {
                return Fail<CancellationPolicyResponse>(
                    "name must be unique.",
                    409);
            }
            var errors = await ValidatePolicy(
                r.ValidFrom,
                r.ValidTo,
                r.CancellationFeePercentage,
                r.FreeCancellationHours,
                r.CutoffHours,
                r.Rules,
                r.Name,
                propertyId,
                ct);

            if (errors.Count > 0)
            {
                return Fail<CancellationPolicyResponse>(
                    "Invalid cancellation policy.",
                    400,
                    errors);
            }

            var now = DateTimeOffset.UtcNow;

            var policy = new CancellationPolicy
            {
                PropertyId = propertyId,
                Name = Clean(r.Name),
                Description = CleanOptional(r.Description),
                CurrentVersion = 1,
                Status = PolicyStatus.Active,
                CreatedAt = now,
                CreatedBy = actor.Id
            };

            var version = new CancellationPolicyVersion
            {
                CancellationPolicy = policy,
                Version = 1,
                ValidFrom = r.ValidFrom.ToUniversalTime(),
                ValidTo = r.ValidTo?.ToUniversalTime(),
                Rules = string.IsNullOrWhiteSpace(r.Rules)
                    ? "{}"
                    : r.Rules,
                FreeCancellationHours = r.FreeCancellationHours,
                CancellationFeePercentage =
                    r.CancellationFeePercentage,
                FixedCancellationFee =
                    r.FixedCancellationFee,
                IsNonRefundable =
                    r.IsNonRefundable,
                CutoffHours =
                    r.CutoffHours,
                CreatedAt = now,
                CreatedBy = actor.Id
            };

            policy.Versions.Add(version);

            var audit = Audit(
                entity: "CancellationPolicy",
                action: "CancellationPolicyCreated",
                id: policy.Id.ToString(),
                oldValue: null,
                newValue: new
                {
                    PolicyId = policy.Id,
                    PropertyId = propertyId,
                    PolicyName = policy.Name,
                    Version = version.Version,
                    VersionId = version.Id,
                    ValidFrom = version.ValidFrom,
                    ValidTo = version.ValidTo,
                    FreeCancellationHours =
                        version.FreeCancellationHours,
                    CancellationFeePercentage =
                        version.CancellationFeePercentage,
                    FixedCancellationFee =
                        version.FixedCancellationFee,
                    IsNonRefundable =
                        version.IsNonRefundable,
                    CutoffHours =
                        version.CutoffHours
                },
                reason: null,
                propertyId: propertyId);

            try
            {
                await _repo.AddCancellationPolicyWithAuditAsync(
                    policy,
                    audit,
                    ct);
            }
            catch (DbUpdateException)
            {
                return Fail<CancellationPolicyResponse>(
                    "The cancellation policy could not be created.",
                    409);
            }

            return Created(
                ToResponse(policy));
        }

        public async Task<ResponseStatus<CancellationPolicyResponse>>
            UpdateCancellationPolicyAsync(
                User actor,
                int propertyId,
                int policyId,
                UpdateCancellationPolicyRequest request,
                string ifMatch,
                CancellationToken ct = default)
        {
            if (actor is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Authenticated user is required.",
                    401);
            }

            if (request is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Request body is required.",
                    400);
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return Fail<CancellationPolicyResponse>(
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
                return Fail<CancellationPolicyResponse>(
                    authorization.Message,
                    authorization.StatusCode);
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

            if (policy.Status == PolicyStatus.Inactive)
            {
                return Fail<CancellationPolicyResponse>(
                    "Cannot update an inactive cancellation policy.",
                    409);
            }

            if (!CheckRowVersion(
                    ifMatch,
                    policy.RowVersion))
            {
                return Fail<CancellationPolicyResponse>(
                    "Concurrency conflict. The cancellation policy was modified by another user.",
                    409);
            }

            var previousVersion =
                policy.Versions
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();

            if (previousVersion is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "The cancellation policy has no previous version.",
                    409);
            }

            var validationErrors =
                await ValidateCancellationUpdateAsync(
                    request,
                    previousVersion,
                    ct);

            if (validationErrors.Count > 0)
            {
                return Fail<CancellationPolicyResponse>(
                    "Invalid cancellation policy.",
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
                previousVersion.CancellationPolicyId,
                previousVersion.Version,
                previousVersion.ValidFrom,
                previousVersion.ValidTo,
                previousVersion.Rules,
                previousVersion.FreeCancellationHours,
                previousVersion.CancellationFeePercentage,
                previousVersion.FixedCancellationFee,
                previousVersion.IsNonRefundable,
                previousVersion.CutoffHours
            };

            /*
             * إغلاق الإصدار السابق عند بداية الإصدار الجديد.
             *
             * لا نقوم بمد ValidTo القديمة إذا كانت منتهية قبل
             * بداية الإصدار الجديد.
             */
            if (!previousVersion.ValidTo.HasValue ||
                previousVersion.ValidTo.Value > newValidFrom)
            {
                previousVersion.ValidTo =
                    newValidFrom;
            }

            var nextVersionNumber =
                policy.CurrentVersion + 1;

            var newVersion =
                new CancellationPolicyVersion
                {
                    CancellationPolicyId =
                        policy.Id,

                    Version =
                        nextVersionNumber,

                    ValidFrom =
                        newValidFrom,

                    ValidTo =
                        newValidTo,

                    Rules =
                        string.IsNullOrWhiteSpace(request.Rules)
                            ? "{}"
                            : request.Rules.Trim(),

                    FreeCancellationHours =
                        request.FreeCancellationHours,

                    CancellationFeePercentage =
                        decimal.Round(
                            request.CancellationFeePercentage,
                            2,
                            MidpointRounding.AwayFromZero),

                    FixedCancellationFee =
                        decimal.Round(
                            request.FixedCancellationFee,
                            2,
                            MidpointRounding.AwayFromZero),

                    IsNonRefundable =
                        request.IsNonRefundable,

                    CutoffHours =
                        request.CutoffHours,

                    CreatedAt =
                        now,

                    CreatedBy =
                        actor.Id
                };

            /*
             * لا نعدل Name أو Description في الـ root policy.
             * نحدث فقط رقم الإصدار وبيانات التتبع.
             */
            policy.CurrentVersion =
                nextVersionNumber;

            policy.UpdatedAt =
                now;

            policy.UpdatedBy =
                actor.Id;

            policy.Versions.Add(newVersion);

            await _repo.AddAuditLogAsync(
                Audit(
                    entity: "CancellationPolicy",
                    action: "CancellationPolicyVersionCreated",
                    id: policy.Id.ToString(),
                    oldValue: oldVersionSnapshot,
                    newValue: new
                    {
                        newVersion.Id,
                        newVersion.CancellationPolicyId,
                        newVersion.Version,
                        newVersion.ValidFrom,
                        newVersion.ValidTo,
                        newVersion.Rules,
                        newVersion.FreeCancellationHours,
                        newVersion.CancellationFeePercentage,
                        newVersion.FixedCancellationFee,
                        newVersion.IsNonRefundable,
                        newVersion.CutoffHours
                    },
                    reason: request.Reason.Trim(),
                    propertyId: propertyId),
                ct);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<CancellationPolicyResponse>(
                    "Concurrency conflict. The cancellation policy was modified by another user.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<CancellationPolicyResponse>(
                    "The cancellation policy version could not be created.",
                    409);
            }

            return Ok(
                ToResponse(policy));
        }


        public async Task<ResponseStatus<CancellationPolicyResponse>>
            SetCancellationPolicyStatusAsync(
                User actor,
                int propertyId,
                int policyId,
                bool active,
                ChangeCancellationPolicyStatusRequest request,
                CancellationToken ct = default)
        {
            if (actor is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Authenticated user is required.",
                    401);
            }

            if (request is null)
            {
                return Fail<CancellationPolicyResponse>(
                    "Request body is required.",
                    400);
            }

            var validationErrors =
                ValidateCancellationPolicyStatusRequest(
                    request,
                    active);

            if (validationErrors.Count > 0)
            {
                return Fail<CancellationPolicyResponse>(
                    "Invalid status change request.",
                    400,
                    validationErrors);
            }

            var authorization =
                await AuthorizeAsync(
                    actor.Id,
                    propertyId,
                    ct);

            if (!authorization.Success)
            {
                return Fail<CancellationPolicyResponse>(
                    authorization.Message,
                    authorization.StatusCode);
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

            var requestedStatus =
                active
                    ? PolicyStatus.Active
                    : PolicyStatus.Inactive;

            if (policy.Status == requestedStatus)
            {
                return Fail<CancellationPolicyResponse>(
                    active
                        ? "Cancellation policy is already active."
                        : "Cancellation policy is already inactive.",
                    409);
            }

            if (!CheckRowVersion(
                    request.ExpectedRowVersion,
                    policy.RowVersion))
            {
                return Fail<CancellationPolicyResponse>(
                    "Concurrency conflict. The cancellation policy was modified by another user.",
                    409);
            }

            if (!active)
            {
                var activePoliciesCount =
                    await _repo.CountActiveCancellationPoliciesAsync(
                        propertyId,
                        ct);

                /*
                 * لا نسمح بتعطيل آخر Policy فعالة.
                 * يجب أن تظل هناك Policy فعالة على الأقل
                 * حتى لا تعمل الحجوزات بدون Cancellation Policy.
                 */
                if (activePoliciesCount <= 1)
                {
                    return Fail<CancellationPolicyResponse>(
                        "The last active cancellation policy cannot be deactivated.",
                        409);
                }

                var isUsedByActiveReservation =
                    await _repo
                        .HasActiveReservationsUsingCancellationPolicyAsync(
                            propertyId,
                            policyId,
                            ct);

                if (isUsedByActiveReservation)
                {
                    return Fail<CancellationPolicyResponse>(
                        "The cancellation policy is used by active reservations and cannot be deactivated.",
                        409,
                        new List<string>
                        {
                    "ACTIVE_RESERVATIONS_USE_POLICY"
                        });
                }
            }

            var oldStatus =
                policy.Status;

            var now =
                DateTimeOffset.UtcNow;

            policy.Status =
                requestedStatus;

            policy.UpdatedAt =
                now;

            policy.UpdatedBy =
                actor.Id;

            var auditAction =
                active
                    ? "CancellationPolicyActivated"
                    : "CancellationPolicyDeactivated";

            var auditReason =
                string.IsNullOrWhiteSpace(request.Reason)
                    ? null
                    : request.Reason.Trim();

            await _repo.AddAuditLogAsync(
                Audit(
                    entity: "CancellationPolicy",
                    action: auditAction,
                    id: policy.Id.ToString(),
                    oldValue: new
                    {
                        Status = oldStatus.ToString(),
                        PolicyId = policy.Id,
                        PropertyId = propertyId
                    },
                    newValue: new
                    {
                        Status = policy.Status.ToString(),
                        PolicyId = policy.Id,
                        PropertyId = propertyId
                    },
                    reason: auditReason,
                    propertyId: propertyId),
                ct);

            try
            {
                await _repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<CancellationPolicyResponse>(
                    "Concurrency conflict. The cancellation policy was modified by another user.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<CancellationPolicyResponse>(
                    "The cancellation policy status could not be changed.",
                    409);
            }

            return Ok(
                ToResponse(policy));
        }
        //--------------------------------------
        //private methods
        //---------------------------------------
        private ResponseStatus<T> Ok<T>(T value)
        {
            return new ResponseStatus<T>(
                value,
                statusCode: 200);
        }

        private ResponseStatus<T> Created<T>(T value)
        {
            return new ResponseStatus<T>(
                value,
                statusCode: 201);
        }

        private ResponseStatus<T> Fail<T>(
            string message,
            int statusCode,
            List<string>? errors = null)
        {
            return new ResponseStatus<T>(
                message,
                errors: errors,
                statusCode: statusCode);
        }


        private ResponseStatus<T> Fail<T>(
            ResponseStatus<bool> response)
        {
            return new ResponseStatus<T>(
                response.Message,
                errors: response.Errors,
                statusCode: response.StatusCode);
        }

        private string R(byte[] bytes)
        {
            return Convert.ToBase64String(
                bytes ?? Array.Empty<byte>());
        }
        private CancellationPolicyResponse ToResponse(
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
        private async Task<List<string>>
    ValidateCancellationUpdateAsync(
        UpdateCancellationPolicyRequest request,
        CancellationPolicyVersion previousVersion,
        CancellationToken ct)
        {
            var errors =
                new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name) ||
                request.Name.Trim().Length < 2)
            {
                errors.Add(
                    "Name must contain at least 2 characters.");
            }

            if (request.Name.Trim().Length > 150)
            {
                errors.Add(
                    "Name cannot exceed 150 characters.");
            }

            if (!string.IsNullOrWhiteSpace(request.Description) &&
                request.Description.Length > 1000)
            {
                errors.Add(
                    "Description cannot exceed 1000 characters.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Trim().Length < 2)
            {
                errors.Add(
                    "Reason must contain at least 2 characters.");
            }

            /*
             * كل validations الخاصة بـ Cancellation Policy:
             *
             * - ValidFrom / ValidTo
             * - fee percentage
             * - free hours
             * - cutoff hours
             * - valid JSON rules
             */
            errors.AddRange(
                await ValidatePolicy(
                    from: request.ValidFrom,
                    to: request.ValidTo,
                    fee: request.CancellationFeePercentage,
                    free: request.FreeCancellationHours,
                    cutoff: request.CutoffHours,
                    rules: request.Rules,
                    name: request.Name,
                    propertyid: previousVersion.CancellationPolicyId,
                    ct: ct));

            errors.AddRange(
                ValidateNewVersionDates(
                    newValidFrom: request.ValidFrom,
                    newValidTo: request.ValidTo,
                    previousValidFrom: previousVersion.ValidFrom,
                    previousValidTo: previousVersion.ValidTo,
                    policyName: "cancellation policy"));

            return errors
                .Distinct()
                .ToList();
        }
        private async Task<List<string>> ValidatePolicy(
          DateTimeOffset from,
          DateTimeOffset? to,
          decimal fee,
          int free,
          int cutoff,
          string rules, string name, int propertyid, CancellationToken ct)
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
            if (to <= from)
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
        private List<string> ValidateDates(
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
    }
}
