using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
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
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.implementation
{


    public sealed class RoomTypeService : IRoomTypeService
    {
        private readonly IUserRepository userRepository;
        private readonly IRoomTypeRepository _repository;

        public RoomTypeService(IUserRepository userRepository,IRoomTypeRepository repository)
        {
            this.userRepository = userRepository;
            _repository = repository;
        }

        public async Task<ResponseStatus<List<RoomTypeResponse>>> GetAllAsync(User actor, RoomTypeListRequest request, CancellationToken ct = default)
        {
            //check actor must be   admin
            var auth = await IsAdminAsync(actor.Id, ct);
            if (!auth.Success)
                return Fail<List<RoomTypeResponse>>("You are not authorized to access this property.", 403);

            var rows = await _repository.GetAllAsync(request.PropertyId ?? actor.PropertyId, request.IncludeInactive, ct);
            return Ok(rows.Select(Map).ToList());
        }

        public async Task<ResponseStatus<RoomTypeResponse>> GetAsync(User actor, int id, CancellationToken ct = default)
        {
            var roomType = await _repository.GetByIdAsync(id, false, ct);
            if (roomType is null)
                return Fail<RoomTypeResponse>("Room type was not found.", 404);
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id, id, ct);
            if (!auth.Success)
                return Fail<RoomTypeResponse>("You are not authorized to access this property.", 403);
            return Ok(Map(roomType));
        }

        public async Task<ResponseStatus<RoomTypeResponse>> 
            CreateAsync(
            User actor,int PropertyId, CreateRoomTypeRequest request, CancellationToken ct = default)
        {
            if (actor is null)
            {
                return Fail<RoomTypeResponse>("User is not authenticated.", 401);
            }
           //check porperty
            var property = await _repository.GetPropertyAsync(PropertyId, ct);
            if (property is null)
                return Fail<RoomTypeResponse>("Property was not found.", 404);
            //check property state must be active
            if (property.Status != PropertyStatus.Active)
            {
                return Fail<RoomTypeResponse>("Room types cannot be created under an inactive property.", 400);
            }
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id, PropertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomTypeResponse>("You are not authorized to access this property.", 403);
            }
                //validation values
            var validation = ValidateValues(request.Name, request.MaxAdults, request.MaxChildren, request.BasePrice);
            if (validation.Count > 0)
            {
                return Fail<RoomTypeResponse>("Room type validation failed.", 400, validation);
            }
            if (await _repository.NameExistsAsync(request.Name, PropertyId, null, ct))
            {
                return Fail<RoomTypeResponse>("A room type with the same name already exists in this property.", 409);
            }
            var roomType = new RoomType
            {
                Name = request.Name.Trim(),
                MaxAdults = request.MaxAdults,
                MaxChildren = request.MaxChildren,
                BasePrice = request.BasePrice,
                Description = request.Description,
                property_id = PropertyId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor.Id
            };
            _ = roomType.Property;
            try
            {
                // The repository persists the tracked entity and its audit record atomically.
                await _repository.SaveAsync(roomType, Audit(actor, roomType, "Create", null, roomType), ct);
                return new ResponseStatus<RoomTypeResponse>(Map(roomType), statusCode: 201);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<RoomTypeResponse>("The room type could not be saved because it was changed by another user.", 409);
            }
        }

        public async Task<ResponseStatus<RoomTypeResponse>> UpdateAsync(User actor, string expectedRowVersion, int id, UpdateRoomTypeRequest request, CancellationToken ct = default)
        {
            var roomType = await _repository.GetByIdAsync(id, true, ct);
            if (roomType is null)
                return Fail<RoomTypeResponse>("Room type was not found.", 404);
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id, id, ct);
            if (!auth.Success)
                return Fail<RoomTypeResponse>("You are not authorized to access this property.", 403);
            var validation = ValidateValues(request.Name, request.MaxAdults, request.MaxChildren, request.BasePrice);
            if (validation.Count > 0)
                return Fail<RoomTypeResponse>("Room type validation failed.", 400, validation);
            if (await _repository.NameExistsAsync(request.Name, roomType.property_id, id, ct))
                return Fail<RoomTypeResponse>("A room type with the same name already exists in this property.", 409);
            if (!TrySetConcurrency(roomType, expectedRowVersion, out var error))
                return Fail<RoomTypeResponse>(error!, 409);

            var old = Map(roomType);
            roomType.Name = request.Name.Trim();
            roomType.MaxAdults = request.MaxAdults;
            roomType.MaxChildren = request.MaxChildren;
            roomType.BasePrice = request.BasePrice;
            roomType.Description = request.Description;
            roomType.UpdatedAt = DateTimeOffset.UtcNow;
            roomType.UpdatedBy = actor.Id;
            try
            {
                await _repository.SaveAsync(roomType, Audit(actor, roomType, "Update", old, roomType), ct);
                return Ok(Map(roomType));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<RoomTypeResponse>("The room type was changed by another user.", 409);
            }
        }

        public async Task<ResponseStatus<RoomTypeResponse>> DeactivateAsync(User actor, string expectedRowVersion, int id, CancellationToken ct = default)
        {
            var roomType = await _repository.GetByIdAsync(id, true, ct);
            if (roomType is null)
                return Fail<RoomTypeResponse>("Room type was not found.", 404);
            //check actor must be manager or admin
            var auth = await AuthorizeAsync(actor.Id, id, ct);
            if (!auth.Success)
                return Fail<RoomTypeResponse>("You are not authorized to access this property.", 403);
            if (!TrySetConcurrency(roomType, expectedRowVersion, out var error))
                return Fail<RoomTypeResponse>(error!, 409);
            var old = Map(roomType);
            roomType.IsActive = false;
            roomType.UpdatedAt = DateTimeOffset.UtcNow;
            roomType.UpdatedBy = actor.Id;
            try
            {
                await _repository.SaveAsync(roomType, Audit(actor, roomType, "Deactivate", old, roomType), ct);
                return Ok(Map(roomType));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<RoomTypeResponse>("The room type was changed by another user.", 409);
            }
        }
        //-----------------------------------------------------------------------
        //private method
        //-----------------------------------------------------------------------

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
        private static bool CanAccessProperty(User actor, int? propertyId) =>
            actor is not null && (!actor.PropertyId.HasValue || !propertyId.HasValue || actor.PropertyId == propertyId);

        private static List<string> ValidateValues(string name, int maxAdults, int maxChildren, decimal basePrice)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(name)) errors.Add("Name is required.");
            if (maxAdults <= 0) errors.Add("Maximum occupancy must be greater than zero.");
            if (maxChildren < 0) errors.Add("Maximum children cannot be negative.");
            if (basePrice < 0) errors.Add("Base price cannot be negative.");
            return errors;
        }

        private static bool TrySetConcurrency(RoomType roomType, string expected, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expected))
            {
                error = "If-Match row version is required.";
                return false;
            }
            try
            {
                roomType.RowVersion = Convert.FromBase64String(expected);
                return true;
            }
            catch (FormatException)
            {
                error = "If-Match row version is invalid.";
                return false;
            }
        }

        private static AddAuditLogDto Audit(User actor, RoomType entity, string action, object? oldValues, object? newValues) => new()
        {
            userid = actor.Id,
            PropertyId = entity.property_id,
            TargetEntity = nameof(RoomType),
            TargetEntityId = entity.Id.ToString(),
            Action = action,
            OldValues = oldValues,
            NewValues = newValues
        };

        private static RoomTypeResponse Map(RoomType x) => new()
        {
            Id = x.Id,
            PropertyId = x.property_id,
            Name = x.Name,
            MaxAdults = x.MaxAdults,
            MaxChildren = x.MaxChildren,
            BasePrice = x.BasePrice,
            Description = x.Description,
            IsActive = x.IsActive,
            RowVersion = Convert.ToBase64String(x.RowVersion ?? Array.Empty<byte>())
        };

        private static ResponseStatus<T> Ok<T>(T value) => new(value);
        private static ResponseStatus<T> Fail<T>(string message, int status, List<string>? errors = null) =>
            new(message: message, errors: errors, statusCode: status);
    }


}
