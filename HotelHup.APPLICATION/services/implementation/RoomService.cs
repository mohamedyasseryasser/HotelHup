using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.room;
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

namespace HotelHup.APPLICATION.services.implementation        
{

    public sealed class RoomService : IRoomService                           
    {
        private readonly IUserRepository _users;
        private readonly IRoomRepository _rooms;
        public RoomService(IUserRepository users, IRoomRepository rooms) { _users = users; _rooms = rooms; }

        public async Task<ResponseStatus<PagedResponse<RoomListItemResponse>>>
            GetListAsync(User actor, RoomListRequest request, CancellationToken ct = default)
        {
            var propertyId = request.PropertyId ?? actor.PropertyId;
            if (!propertyId.HasValue) return Fail<PagedResponse<RoomListItemResponse>>("PropertyId is required.", 400);
            var auth = await AuthorizeAsync(actor, propertyId.Value, ct);
            if (!auth.Success) return Fail<PagedResponse<RoomListItemResponse>>(auth.Message, auth.StatusCode);
            var property = await _rooms.GetPropertyAsync(propertyId.Value, ct);
            if (property is null) return Fail<PagedResponse<RoomListItemResponse>>("Property was not found.", 404);
            var result = await _rooms.GetListAsync(propertyId.Value, request.Search, request.IncludeInactive, request.PageNumber, request.PageSize, ct);

            return Ok(new PagedResponse<RoomListItemResponse>
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = result.TotalCount,
                Items = result.Items
             .Select(r => new RoomListItemResponse
             {
                 Id = r.Id,
                 PropertyId = r.property_id,
                 RoomTypeId = r.RoomTypeId,
                 RoomNumber = r.RoomNumber,
                 Status = r.Status,
                 IsActive = r.IsActive,
                 PricePerNight = r.PricePerNight,
                 RowVersion = Convert.ToBase64String(
                     r.RowVersion ?? Array.Empty<byte>())
             })
             .ToList()
            });



        }

        public async Task<ResponseStatus<RoomResponse>>
            GetAsync(User actor, int propertyId, int id, CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(actor, propertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomResponse>(auth.Message, auth.StatusCode);
            }
            var room = await _rooms.GetByIdAsync(propertyId, id, false, ct);
            return room is null ? Fail<RoomResponse>("Room was not found.", 404) : Ok(Map(room));
        }

        public async Task<ResponseStatus<RoomResponse>> 
            CreateAsync(User actor, 
            int propertyId, 
            CreateRoomRequest request, 
            CancellationToken ct = default)
        {
            //authorized
            var auth = await AuthorizeAsync(actor, propertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomResponse>(auth.Message, auth.StatusCode);
            }
            //must property not null and is active
            var property = await _rooms.GetPropertyAsync(propertyId, ct);
            if (property is null)
            {
                return Fail<RoomResponse>("Property was not found.", 404);
            }
            if (property.Status != PropertyStatus.Active)
            {
                return Fail<RoomResponse>("Rooms cannot be created under an inactive property.", 400);
            }
            var number = request.RoomNumber.Trim();
            if (await _rooms.RoomNumberExistsAsync(propertyId, number, null, ct))
            {
                return Fail<RoomResponse>("A room with the same number already exists in this property.", 409);
            }
            var priceError = ValidatePrice(request.PricePerNight);

            if (priceError is not null)
            {
                return Fail<RoomResponse>(
                    priceError,
                    400);
            }
            var type = await _rooms.GetRoomTypeAsync(propertyId, request.RoomTypeId, true, ct);
            if (type is null)
            {
                return Fail<RoomResponse>("Active RoomType was not found in this property.", 400);
            }
            var room = new Room 
            { 
                PricePerNight=request.PricePerNight,
                property_id = propertyId,
                RoomNumber = number,
                RoomTypeId = request.RoomTypeId,
                Status = RoomStatus.Available,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor.Id 
            };
            try
            {
                await _rooms.PersistAsync(room, Audit(actor, propertyId, room.Id.ToString(), "Create", null, room));
                return new ResponseStatus<RoomResponse>(Map(room), statusCode: 201);
            }
            catch (DbUpdateException) { return Fail<RoomResponse>("The room conflicts with existing data.", 409); }
        }

        public async Task<ResponseStatus<RoomResponse>>
            UpdateAsync(User actor,
            int propertyId,
            int id,
            UpdateRoomRequest request,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(actor, propertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomResponse>(auth.Message, auth.StatusCode);
            }
            var room = await _rooms.GetByIdAsync(propertyId, id, true, ct);
            if (room is null)
            {
                return Fail<RoomResponse>("Room was not found.", 404);
            }
            if (!room.IsActive)
            {
                return Fail<RoomResponse>("Inactive rooms cannot be updated.", 400);
            }
            if (!TryDecode(request.ExpectedRowVersion, room, out var versionError))
            {
                return Fail<RoomResponse>(versionError!, 409);
            }
            var number = request.RoomNumber.Trim();
            if (await _rooms.RoomNumberExistsAsync(propertyId, number, id, ct))
            {
                return Fail<RoomResponse>("A room with the same number already exists in this property.", 409);
            }
            var priceError = ValidatePrice(request.PricePerNight);

            if (priceError is not null)
            {
                return Fail<RoomResponse>(
                    priceError,
                    400);
            }
            if (request.RoomTypeId != room.RoomTypeId)
            {
                if (await _rooms.HasActiveReservationAsync(propertyId, id, ct))
                {
                    return Fail<RoomResponse>("RoomType cannot be changed while the room has an active reservation or stay.", 409);
                }
                if (await _rooms.GetRoomTypeAsync(propertyId, request.RoomTypeId, true, ct) is null)
                {
                    return Fail<RoomResponse>("Active RoomType was not found in this property.", 400);
                }
            }
            var old = Map(room);
            room.RoomNumber = number;
            room.RoomTypeId = request.RoomTypeId;
            room.PricePerNight = request.PricePerNight;
            room.UpdatedAt = DateTimeOffset.UtcNow; 
            room.UpdatedBy = actor.Id;
            try
            {
                await _rooms.PersistAsync(room, Audit(actor, propertyId, id.ToString(), "Update", old, room));
                return Ok(Map(room));
            }
            catch (DbUpdateConcurrencyException) { return Fail<RoomResponse>("The room was updated by another user.", 409); }
            catch (DbUpdateException) { return Fail<RoomResponse>("The room update conflicts with existing data.", 409); }
        }

        public async Task<ResponseStatus<RoomResponse>>
            DeactivateAsync(User actor, 
            int propertyId, 
            int id, 
            DeactivateRoomRequest request,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(actor, propertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomResponse>(auth.Message, auth.StatusCode);
            }
            var room = await _rooms.GetByIdAsync(propertyId, id, true, ct);
            if (room is null)
            {
                return Fail<RoomResponse>("Room was not found.", 404);
            }
            if (!TryDecode(request.ExpectedRowVersion, room, out var versionError))
            {
                return Fail<RoomResponse>(versionError!, 409);
            }
            if (!room.IsActive)
            {
                return Fail<RoomResponse>("Room is already inactive.", 400);
            }
            if (await _rooms.HasActiveReservationAsync(propertyId, id, ct))
            {
                return Fail<RoomResponse>("A room with an active reservation or stay cannot be deactivated.", 409);
            }
            var old = Map(room); 
            room.IsActive = false;
            room.UpdatedAt = DateTimeOffset.UtcNow; 
            room.UpdatedBy = actor.Id;
            try { await _rooms.PersistAsync(room, Audit(actor, propertyId, id.ToString(), "Deactivate", old, room)); return Ok(Map(room)); }
            catch (DbUpdateConcurrencyException)
            { 
                return Fail<RoomResponse>("The room was updated by another user.", 409);
            }
        }

        public async Task<ResponseStatus<RoomResponse>>
            ActivateAsync(User actor,
            int propertyId,
            int id,
            DeactivateRoomRequest request,
            CancellationToken ct = default)
        {
            var auth = await AuthorizeAsync(actor, propertyId, ct);
            if (!auth.Success)
            {
                return Fail<RoomResponse>(auth.Message, auth.StatusCode);
            }
            var room = await _rooms.GetByIdAsync(propertyId, id, true, ct);
            if (room is null)
            {
                return Fail<RoomResponse>("Room was not found.", 404);
            }
            if (!TryDecode(request.ExpectedRowVersion, room, out var versionError))
            {
                return Fail<RoomResponse>(versionError!, 409);
            }
            if (room.IsActive)
            {
                return Fail<RoomResponse>("Room is already activate.", 400);
            }
            if (await _rooms.HasActiveReservationAsync(propertyId, id, ct))
            {
                return Fail<RoomResponse>("A room with an active reservation or stay cannot be deactivated.", 409);
            }
            var old = Map(room);
            room.IsActive = true;
            room.UpdatedAt = DateTimeOffset.UtcNow;
            room.UpdatedBy = actor.Id;
            try { await _rooms.PersistAsync(room, Audit(actor, propertyId, id.ToString(), "Deactivate", old, room)); return Ok(Map(room)); }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<RoomResponse>("The room was updated by another user.", 409);
            }
        }
        //-----------------------------------------------------------------------------------
        //private method
        //-----------------------------------------------------------------------------------
        private async Task<ResponseStatus<AuthorizationDataDto>> IsAdminAsync(
          string actorId,
       CancellationToken cancellationToken = default)
        {
            var actor = await _users.GetByIdAsync(
                actorId,
                cancellationToken);

            if (actor is null)
            {
                return new ResponseStatus<AuthorizationDataDto>(
                    message: "User not found.",
                    statusCode: 404);
            }

            var roles = await _users.GetRolesAsync(
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
       User actor,
       int? propertyId,
       CancellationToken cancellationToken = default)
        {
            var result = await IsAdminAsync(
                actor.Id,
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
            //    var isManager = data.Roles.Any(role =>
            //      role.IsActive &&
            //    string.Equals(
            //      role.Name,
            //    nameof(UserRole.Manager),
            //  StringComparison.OrdinalIgnoreCase));

            //   if (!isManager)
            // {
            //   return new ResponseStatus<bool>(
            //     message: "Admin or Manager access required.",
            //   statusCode: 403);
            //}

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

        private static bool TryDecode(string expected, Room room, out string? error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expected)) { error = "ExpectedRowVersion is required."; return false; }
            try
            {
                var supplied = Convert.FromBase64String(expected);
                if (!supplied.SequenceEqual(room.RowVersion ?? Array.Empty<byte>())) 
                { error = "The room was modified by another user."; return false; }
                room.RowVersion = supplied;
                return true;
            }
            catch (FormatException) { error = "ExpectedRowVersion is invalid."; return false; }
        }

        private static AuditLog Audit(User actor, int propertyId, string entityId, string action, object? oldValues, object? newValues) => new()
        {
            UserId = actor.Id,
            PropertyId = propertyId,
            EntityName = nameof(Room),
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor.Id
        };
        private static string? ValidatePrice(decimal price)
        {
            if (price <= 0)
            {
                return "Price per night must be greater than zero.";
            }

            if (price > 99999999999999.99m)
            {
                return "Price per night is too large.";
            }

            return null;
        }
        private static RoomResponse Map(Room x) => new() 
        { 
            
            Id = x.Id,
            PricePerNight=x.PricePerNight,
            PropertyId = x.property_id, 
            RoomTypeId = x.RoomTypeId,
            RoomNumber = x.RoomNumber,
            Status = x.Status,
            IsActive = x.IsActive, 
            RowVersion = Convert.ToBase64String(x.RowVersion ?? Array.Empty<byte>())
        };
        private static ResponseStatus<T> Ok<T>(T value) => new(value);
        private static ResponseStatus<T> Fail<T>(string message, int status) => new(message: message, statusCode: status);
    }

}
