using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.RatePlane;
using HotelHup.APPLICATION.DTO.RoomType;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static HotelHup.APPLICATION.Constant.Permissions;

namespace HotelHup.APPLICATION.services.implementation
{

    public sealed class RatePlanService : IRatePlanService
    {
        private readonly IRoomTypeRepository roomTypeRepository;
        private readonly IPropertyRepository propertyRepository;
        private readonly IUserRepository userRepository;
        private readonly IRatePlanRepository _repository;
        public RatePlanService(IRoomTypeRepository roomTypeRepository,IPropertyRepository propertyRepository,IUserRepository userRepository,IRatePlanRepository repository)
        {
            this.roomTypeRepository = roomTypeRepository;
            this.propertyRepository = propertyRepository;
            this.userRepository = userRepository;
            _repository = repository;
        }

        public async Task<ResponseStatus<List<RatePlanResponse>>> 
            GetAllAsync(User actor,int propertyid,
            CancellationToken ct = default)
        {
            var property = await propertyRepository.GetByIdAsync(propertyid,ct);
            if (property == null)
            {
                return Fail<List<RatePlanResponse>>(message:"this property is not found",status:404);
            }
            var plans = await _repository.GetAllAsync(property.ID,ct);
            var auth = await AuthorizeAsync(actor.Id, propertyid, ct);
            if (!auth.Success)
                return Fail<List< RatePlanResponse>>(auth.Message, auth.StatusCode);
            return Ok(plans.Select(x=> Map(x)).ToList());
        }

        public async Task<ResponseStatus<RatePlanResponse>>
            GetAsync(User actor, 
            int id,
            CancellationToken ct = default)
        {
            var plan = await _repository.GetByIdAsync(id, false, ct);
            if (plan is null) return Fail<RatePlanResponse>("Rate plan was not found.", 404);
            var auth = await AuthorizeAsync(actor.Id, plan.PropertyId, ct);
            if (!auth.Success)
                return Fail<RatePlanResponse>(auth.Message, auth.StatusCode); 
            return Ok(Map(plan));
        }

        public async Task<ResponseStatus<RatePlanResponse>> 
            CreateAsync(
            User actor,
            int propertyid,
            int roomtypeid, 
            CreateRatePlanRequest request,
            CancellationToken ct = default)
        {           
           //must property is active
           //must roomtype belong same propertyid
           //must actor be admin or manager
            var access = await ValidateTargetAsync(actor, propertyid, roomtypeid, ct);
            if (access is not null)
            {
                return Fail<RatePlanResponse>(access.Message, access.StatusCode);
            }

            var validation = ValidateVersionValues(request.Price, request.ValidFrom, request.ValidTo, request.Name);
            if (validation.Count > 0)
            {
                return Fail<RatePlanResponse>("Rate plan validation failed.", 400, validation);
            }
            var plan = new RatePlan
            {
                PropertyId = propertyid,
                RoomTypeId = roomtypeid,
                Name = request.Name.Trim(),
                Type = request.Type,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor.Id
            };
            var version = 
                NewVersion
                (plan, 1, request.Price, request.Rules, request.IsRefundable, request.ValidFrom, request.ValidTo, actor.Id);
            try
            {
                await _repository.PersistAsync(plan, version, Audit(actor, plan, "Create", null, Map(version)), ct);
                return new ResponseStatus<RatePlanResponse>(Map(plan, version), statusCode: 201);
            }
            catch (DbUpdateConcurrencyException) { return Fail<RatePlanResponse>("The rate plan could not be saved because of a concurrency conflict.", 409); }
            catch (DbUpdateException) { return Fail<RatePlanResponse>("The rate plan could not be saved.", 409); }
        }

        public async Task<ResponseStatus<RatePlanResponse>> 
            UpdateAsync(
            User actor,
            string expectedRowVersion,
            int id,
            UpdateRatePlanRequest request,
            CancellationToken ct = default)
        {
            //check plan exit
            var plan = await _repository.GetByIdAsync(id, true, ct);
            if (plan is null)
            {
                return Fail<RatePlanResponse>("Rate plan was not found.", 404);
            }
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id,plan.PropertyId, ct);
            if (!auth.Success)
            {
                return Fail<RatePlanResponse>
                    ("You are not authorized to create rate plane.", 403);
            }
            var validation = ValidateVersionValues(request.Price, request.ValidFrom, request.ValidTo, request.Name);
            if (validation.Count > 0)
            {
                return Fail<RatePlanResponse>("Rate plan validation failed.", 400, validation);
            }
            if (!CheckRowVersion(expectedRowVersion, plan.RowVersion))
            {
                return Fail<RatePlanResponse>("Concurrency conflict", 409);
            }
            var old = Map(plan);
            var latest = await _repository.GetLatestVersionAsync(id, ct);
            var versionChanged = latest is null || latest.Price != request.Price || latest.Rules != request.Rules ||
                latest.IsRefundable != request.IsRefundable || latest.ValidFrom != request.ValidFrom || latest.ValidTo != request.ValidTo;
            RatePlanVersion? newVersion = null;
            if (versionChanged)
            {
                var next = (latest?.VersionNumber ?? 0) + 1;
                newVersion = NewVersion(plan, next, request.Price, request.Rules, request.IsRefundable, request.ValidFrom, request.ValidTo, actor.Id);
            }
            plan.Name = request.Name.Trim();
            plan.Type = request.Type;
            plan.UpdatedAt = DateTimeOffset.UtcNow;
            plan.UpdatedBy = actor.Id;
            try
            {
                await _repository.PersistAsync(plan, newVersion, Audit(actor, plan, "Update", old, newVersion is null ? Map(plan) : Map(newVersion)), ct);
                return Ok(Map(plan, newVersion ?? latest));
            }
            catch (DbUpdateConcurrencyException) { return Fail<RatePlanResponse>("The rate plan was changed by another user.", 409); }
            catch (DbUpdateException) { return Fail<RatePlanResponse>("The rate plan update conflicts with existing data.", 409); }
        }

        public async Task<ResponseStatus<List<RatePlanVersionResponse>>> 
            GetVersionsAsync(User actor,
            int id,
            CancellationToken ct = default)
        {
            var plan = await _repository.GetByIdAsync(id, false, ct);
            if (plan is null) return Fail<List<RatePlanVersionResponse>>("Rate plan was not found.", 404);
            var auth = await AuthorizeAsync(actor.Id, plan.PropertyId, ct);
            if (!auth.Success)
                return Fail<List<RatePlanVersionResponse>>(auth.Message, auth.StatusCode);
            var versions = await _repository.GetVersionsAsync(id, ct);
                return Ok(versions.Select(x => Map(x)).ToList());
        }
        

        public async Task<ResponseStatus<RatePlanVersionResponse>> GetVersionAsync(User actor, int id, int versionNumber, CancellationToken ct = default)
        {
            var plan = await _repository.GetByIdAsync(id, false, ct);
            if (plan is null) return Fail<RatePlanVersionResponse>("Rate plan was not found.", 404);
            var auth = await AuthorizeAsync(actor.Id, plan.PropertyId, ct);
            if (!auth.Success)
                return Fail<RatePlanVersionResponse>(auth.Message, auth.StatusCode); 
            var version = await _repository.GetVersionAsync(id, versionNumber, ct);
            return version is null ? Fail<RatePlanVersionResponse>("Rate plan version was not found.", 404) : Ok(Map(version));
        }

        public async Task<ResponseStatus<RatePlanResponse>> DeactivateAsync(User actor, string expectedRowVersion, int id, CancellationToken ct = default)
        {
            var plan = await _repository.GetByIdAsync(id, true, ct);
            if (plan is null) return Fail<RatePlanResponse>("Rate plan was not found.", 404);
            var property = await propertyRepository.GetByIdAsync(plan.PropertyId,ct);
            if (property == null ||property.Status==PropertyStatus.Inactive)
            {
                return Fail<RatePlanResponse>(message: "this property is not found", status: 404);
            }
            var auth = await AuthorizeAsync(actor.Id, plan.PropertyId, ct);
            if (!auth.Success)
                return Fail<RatePlanResponse>(auth.Message, auth.StatusCode);

            if (!CheckRowVersion(expectedRowVersion, plan.RowVersion))
            {
                return Fail<RatePlanResponse>("Concurrency conflict", 409);
            }
            var old = Map(plan);
            plan.IsActive = false; plan.UpdatedAt = DateTimeOffset.UtcNow; plan.UpdatedBy = actor.Id;
            try
            {
                await _repository.PersistAsync(plan, null, Audit(actor, plan, "Deactivate", old, Map(plan)), ct);
                return Ok(Map(plan));
            }
            catch (DbUpdateConcurrencyException) { return Fail<RatePlanResponse>("The rate plan was changed by another user.", 409); }
        }
        //-------------------------------------------------------------------------------------------
        //private methods
        //-------------------------------------------------------------------------------------------
        private async Task<ResponseStatus<bool>?> ValidateTargetAsync(User actor, int propertyId, int roomTypeId, CancellationToken ct)
        {
            var property =await propertyRepository.GetByIdAsync(propertyId,ct);
            if (property is null)
            {
                return new ResponseStatus<bool>(message: "Property was not found.",
                    statusCode: 404);
            }
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id, property.ID, ct);
            if (!auth.Success)
            {
                return Fail<bool>
                    ("You are not authorized to create rate plane.", 403);
            }
            if (property.Status != PropertyStatus.Active)
            {
                return new ResponseStatus<bool>
                    (message: "Rate plans cannot be created under an inactive property.",
                    statusCode: 400);
            }
            var roomType = await roomTypeRepository.GetByIdAsync(roomTypeId,false, ct);
            if (roomType is null)
            {
                return new ResponseStatus<bool>(message: "Room type was not found.", statusCode: 404);
            }
            if (roomType.property_id != propertyId)            
            {
                return new ResponseStatus<bool>(message: "Room type does not belong to the selected property.", statusCode: 400);
            }
            if (!roomType.IsActive)
            {
                return new ResponseStatus<bool>(message: "Rate plans cannot be created for an inactive room type.", statusCode: 400);
            } 
            return null;
        }
        private static List<string> ValidateVersionValues(decimal price, DateTime from, DateTime? to, string name)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(name)) errors.Add("Name is required.");
            if (price < 0) errors.Add("Price cannot be negative.");
            if (to.HasValue && to.Value < from) errors.Add("ValidTo cannot precede ValidFrom.");
            return errors;
        }
        
        private static RatePlanVersion NewVersion(RatePlan plan, int number, decimal price, string rules, bool refundable, DateTime from, DateTime? to, string userId) => new()
        {
            RatePlan = plan,
            RatePlanId = plan.Id,
            VersionNumber = number,
            Price = price,
            Rules = rules,
            IsRefundable = refundable,
            ValidFrom = from,
            ValidTo = to,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId
        };
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
        private static AddAuditLogDto Audit(User actor, RatePlan plan, string action, object? oldValue, object? newValue) => new()
        {
            userid = actor.Id,
            PropertyId = plan.PropertyId,
            TargetEntity = nameof(RatePlan),
            TargetEntityId = plan.Id.ToString(),
            Action = action,
            OldValues = oldValue,
            NewValues = newValue
        };
        private static RatePlanResponse Map(RatePlan x, RatePlanVersion? version = null) => new()
        {
            Id = x.Id,
            PropertyId = x.PropertyId,
            RoomTypeId = x.RoomTypeId,
            Name = x.Name,
            Type = x.Type,
            IsActive = x.IsActive,
            RowVersion = Convert.ToBase64String(x.RowVersion ?? Array.Empty<byte>()),
            CurrentVersion = Map(version ?? x.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault())
        };
        private static RatePlanVersionResponse? Map(RatePlanVersion? x) => x is null ? null : new()
        {
            Id = x.Id,
            RatePlanId = x.RatePlanId,
            VersionNumber = x.VersionNumber,
            Price = x.Price,
            Rules = x.Rules,
            IsRefundable = x.IsRefundable,
            ValidFrom = x.ValidFrom,
            ValidTo = x.ValidTo,
            IsActive = x.IsActive
        };
        private static ResponseStatus<T> Ok<T>(T value)
        {
            return new ResponseStatus<T>(
                value,
                statusCode: 200);
        }
        private static ResponseStatus<T> Fail<T>(string message, int status, List<string>? errors = null) => new(message: message, errors: errors, statusCode: status);
        private async Task<ResponseStatus<AuthorizationDataDto>> IsAdminAsync(
        string actorId,
        CancellationToken cancellationToken = default)
        {
            var actor = await userRepository.GetByIdAsync(                  
                actorId,
                cancellationToken);

            if (actor is null)
            {
                return new ResponseStatus<AuthorizationDataDto>(            
                    message: "User not found.",                                   
                    statusCode: 404);                                             
            }

            var roles = await userRepository.GetRolesAsync(                      
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
    }

}
