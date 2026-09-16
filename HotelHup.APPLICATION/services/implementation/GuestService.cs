using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.guest;
using HotelHup.APPLICATION.DTO.RatePlane;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.services.implementation
{
    public sealed class GuestService : IGuestService
    {
        private readonly IUserRepository userRepository;
        private readonly IGuestRepository _repository;
        private readonly IPropertyRepository propertyRepository;

        public GuestService(IUserRepository userRepository,IGuestRepository repository,IPropertyRepository propertyRepository )
        {
            this.userRepository = userRepository;
            _repository = repository;
            this.propertyRepository = propertyRepository;
        }

        public async Task<ResponseStatus<GuestResponse>> CreateAsync(
       User actor,
       int propertyid,
       CreateGuestRequest request,
       CancellationToken ct = default)
        {
            // Check property
            var property = await propertyRepository.GetByIdAsync(
                propertyid,
                ct);

            if (property is null ||
                property.Status == PropertyStatus.Inactive)
            {
                return new ResponseStatus<GuestResponse>(
                    message: "Property was not found or not active.",
                    statusCode: 400);
            }

            // Check authorization
            var authorization = await AuthorizeAsync(
                actor.Id,
                propertyid,
                ct);

            if (!authorization.Success)
            {
                return Fail<GuestResponse>(
                    authorization.Message,
                     authorization.StatusCode);
            }

            // Check possible duplicates
            var duplicates =
                await _repository.FindPossibleDuplicatesAsync(
                    property.ID,
                    request.Phone,
                    request.Email,
                    request.NationalId,
                    request.IdentityNumber,
                    null,
                    ct);

            if (duplicates.Count > 0)
            {
                return Fail<GuestResponse>(
                    "A possible duplicate guest was found.",
                     409);
            }

            // Create guest
            var guest = new Guest
            {
                propertyid = propertyid,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actor.Id
            };

            Apply(guest, request);

            // Create audit
            var audit = Audit(
                actor,
                property.ID,
                "CreateGuest",
                null,
                guest,
                null);

            // Add guest + audit + save inside transaction
            await _repository.AddAsync(
                guest,
                audit,
                ct);

            return Ok(
                Map(guest),
                201);

            return Ok(
                Map(guest),
                201);
        }

        public async Task<ResponseStatus<GuestResponse>>
            GetByIdAsync(User actor,int propertyid, 
            int id,
            CancellationToken ct = default)
        {
            // Check property
            var property = await propertyRepository.GetByIdAsync(
                propertyid,
                ct);

            if (property is null )
            {
                return new ResponseStatus<GuestResponse>(
                    message: "Property was not found .",
                    statusCode: 400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id,propertyid, ct);
            if (!authorization.Success)
            {
                return Fail<GuestResponse>(authorization.Message, authorization.StatusCode);
            }
            //check exit guest
            var guest = await _repository.GetTrackedByIdAsync(id,propertyid, ct);
            if (guest == null)
            {
                return Fail<GuestResponse>(
                    "Guest not found.",
                     404);
            }
            return Ok(Map(guest));
        }

        public async Task<ResponseStatus<PagedResponse<GuestListItemResponse>>>
            GetListAsync(
            User actor,int propertyid,
            GuestListRequest request,
            CancellationToken ct = default)
        {
            // Check property
            var property = await propertyRepository.GetByIdAsync(
                propertyid,
                ct);

            if (property is null)
            {
                return Fail <PagedResponse<GuestListItemResponse>>(
                     "Property was not found .",
                     400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id, propertyid, ct);
            if (!authorization.Success)
            {
                return Fail <PagedResponse<GuestListItemResponse>> (authorization.Message, authorization.StatusCode);
            }
            

            var result = await _repository.GetListAsync(request,propertyid, ct);
            return Ok(new PagedResponse<GuestListItemResponse>
            {
                Items = result.Items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = result.TotalCount
            });
        }

        public async Task<ResponseStatus<GuestResponse>> UpdateAsync(
            User actor,int propertyid,
            int id,
            UpdateGuestRequest request,
            CancellationToken ct = default)
        {
            //check exit guest
            var guest = await _repository.GetTrackedByIdAsync(id,propertyid, ct);
            if (guest == null)
            {
                return Fail<GuestResponse>(
                    "Guest not found.",
                     404);
            }
            
            // Check property
            var property = await propertyRepository.GetByIdAsync(
                guest.propertyid,
                ct); 
            if (property is null)
            {
                return Fail<GuestResponse>(
                    "Property was not found.",
                    404);
            }
            //must be property is active
            if (property.Status == PropertyStatus.Inactive)
            {
                return new ResponseStatus<GuestResponse>(
                    message: "Property was not active.",
                    statusCode: 400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id, guest.propertyid, ct);
            if (!authorization.Success)
            {
                return Fail<GuestResponse>(authorization.Message, authorization.StatusCode);
            }
            
            if (!CheckRowVersion(request.ExpectedRowVersion, guest.RowVersion))
                return Fail<GuestResponse>("Guest was modified by another user.", 409);

            var duplicates = await _repository.FindPossibleDuplicatesAsync(
                guest.propertyid,
                request.Phone, 
                request.Email, 
                request.NationalId,
                request.IdentityNumber, 
                id,
                ct);
            if (duplicates.Count > 0)
                return Fail<GuestResponse>("A possible duplicate guest was found.", 409);

            var oldValues = Snapshot(guest);
            Apply(guest, request);
            guest.UpdatedAt = DateTimeOffset.UtcNow;
            guest.UpdatedBy = actor.Id;

            try
            {
                await _repository.PersistAsync(guest, 
                    Audit(
                        actor, property.ID,
                    "Update",
                    oldValues,
                    guest, 
                    null
                    ),
                    ct);
                return Ok(Map(guest));
            }
            catch (DbUpdateConcurrencyException) 
            { 
                return Fail<GuestResponse>(
                    "The guest was updated by another user.",
                    409); 
            }
            catch (DbUpdateException) { return Fail<GuestResponse>(
                "The guest update conflicts with existing data.", 
                409);
            }
        }

        public async Task<ResponseStatus<GuestResponse>>
            DeactivateAsync(User actor,int propertyid,
            int id,
            DeactivateGuestRequest request, 
            CancellationToken ct = default)
        {
            //check exit guest
            var guest = await _repository.GetTrackedByIdAsync(id, propertyid,ct);
            if (guest == null)
            {
                return Fail<GuestResponse>(
                    "Guest not found.",
                     404);
            }

            // Check property
            var property = await propertyRepository.GetByIdAsync(
                guest.propertyid,
                ct);
            //must be property is active
            if (property is null)
            {
                return Fail<GuestResponse>(
                    "Property was not found.",
                    404);
            }
            if (property.Status == PropertyStatus.Inactive)
            {
                return new ResponseStatus<GuestResponse>(
                    message: "Property was not active.",
                    statusCode: 400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id, guest.propertyid, ct);
            if (!authorization.Success)
            {
                return Fail<GuestResponse>(authorization.Message, authorization.StatusCode);
            }

            if (!CheckRowVersion(request.ExpectedRowVersion, guest.RowVersion))
                return Fail<GuestResponse>("Guest was modified by another user.", 409);
            if (!guest.IsActive)
                return Fail<GuestResponse>("Guest is already inactive.", 400);

            guest.IsActive = false;
            guest.UpdatedAt = DateTimeOffset.UtcNow;
            guest.UpdatedBy = actor.Id;
            try
            {
                await _repository.PersistAsync(guest,
                    Audit(
                        actor, property.ID,
                    " deactive",
                     new { IsActive = true },
                new { IsActive = false },
                    null
                    ),
                    ct);
                return Ok(Map(guest));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<GuestResponse>(
                    "The guest was updated by another user.",
                    409);
            }
            catch (DbUpdateException)
            {
                return Fail<GuestResponse>(
                "The guest update conflicts with existing data.",
                409);
            }
            
        }

        public async Task<ResponseStatus<GuestResponse>> 
            AnonymizeAsync(User actor,
          int propertyid,  int id,
            AnonymizeGuestRequest request,
            CancellationToken ct = default)
        {
            //check exit guest
            var guest = await _repository.GetTrackedByIdAsync(id, propertyid, ct);
            if (guest == null)
            {
                return Fail<GuestResponse>(
                    "Guest not found.",
                     404);
            }

            // Check property
            var property = await propertyRepository.GetByIdAsync(
                guest.propertyid,
                ct);
            //must be property is active
            if (property is null)
            {
                return Fail<GuestResponse>(
                    "Property was not found.",
                    404);
            }
            if (property.Status == PropertyStatus.Inactive)
            {
                return new ResponseStatus<GuestResponse>(
                    message: "Property was not active.",
                    statusCode: 400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id, guest.propertyid, ct);
            if (!authorization.Success)
            {
                return Fail<GuestResponse>(authorization.Message, authorization.StatusCode);
            }

            if (!CheckRowVersion(request.ExpectedRowVersion, guest.RowVersion))
                return Fail<GuestResponse>("Guest was modified by another user.", 409);
            if (!guest.IsActive)
                return Fail<GuestResponse>("Guest is already inactive.", 400);
            if (!await _repository.HasReservationsAsync(id, ct) && !await _repository.HasFoliosAsync(id, ct))
                return Fail<GuestResponse>("Guest has no historical records requiring anonymization.",  400);

            var oldValues = Snapshot(guest);
            guest.FirstName = "ANONYMIZED";
            guest.LastName = "GUEST";
            guest.IdentityType = null;
            guest.IdentityNumber = null;
            guest.NationalId = null;
            guest.Phone = null;
            guest.Email = null;
            guest.Address = null;
            guest.nationality = string.Empty;
            guest.InternalNotes = null;
            guest.GuestVisibleNotes = null;
            guest.Preferences = null;
            guest.IsActive = false;
            guest.UpdatedAt = DateTimeOffset.UtcNow;
            guest.UpdatedBy = actor.Id;
            try
            {
                await _repository.PersistAsync(guest,
                    Audit(
                        actor, property.ID,
                    " anonymize",
                     oldValues,
                new { Anonymize = true },
                    null
                    ),
                    ct);
                return Ok(Map(guest));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Fail<GuestResponse>(
                    "The guest was updated by another user.",
                    409);
            }
            
        }

        public async Task<ResponseStatus<IReadOnlyList<GuestReservationSummaryResponse>>>
            GetReservationHistoryAsync(User actor, int propertyid,int id, CancellationToken ct = default)
        {
            //check exit guest
            var guest = await _repository.GetTrackedByIdAsync(id, propertyid, ct);
            if (guest == null)
            {
                return Fail<IReadOnlyList<GuestReservationSummaryResponse>>(
                    "Guest not found.",
                     404);
            }

            // Check property
            var property = await propertyRepository.GetByIdAsync(
                guest.propertyid,
                ct);
            //must be property is active
            if (property.Status == PropertyStatus.Inactive)
            {
                return new ResponseStatus<IReadOnlyList<GuestReservationSummaryResponse>>(
                    message: "Property was not active.",
                    statusCode: 400);
            }
            //actor must be admin or receptionist or manager
            var authorization = await AuthorizeAsync(actor.Id, guest.propertyid, ct);
            if (!authorization.Success)
            {
                return Fail<IReadOnlyList<GuestReservationSummaryResponse>>(authorization.Message, authorization.StatusCode);
            }
            return Ok(await _repository.GetReservationHistoryAsync(id, propertyid,ct));
        }

 

        private static void Apply(Guest guest, CreateGuestRequest request)
        {
            guest.FirstName = request.FirstName.Trim();
            guest.LastName = request.LastName.Trim();
            guest.IdentityType = Clean(request.IdentityType);
            guest.IdentityNumber = Clean(request.IdentityNumber);
            guest.NationalId = Clean(request.NationalId);
            guest.Phone = Clean(request.Phone);
            guest.Email = Clean(request.Email)?.ToLowerInvariant();
            guest.Address = Clean(request.Address);
            guest.nationality = Clean(request.Nationality) ?? string.Empty;
            guest.Preferences = Clean(request.Preferences);
            guest.InternalNotes = Clean(request.InternalNotes);
            guest.GuestVisibleNotes = Clean(request.GuestVisibleNotes);
        }

        private static GuestResponse Map(Guest x) => new()
        {
            Id = x.Id,
            propertyid = x.propertyid,
            FirstName = x.FirstName,
            LastName = x.LastName,
            IdentityType = x.IdentityType,
            IdentityNumber = x.IdentityNumber,
            NationalId = x.NationalId,
            Phone = x.Phone,
            Email = x.Email,
            Address = x.Address,
            Nationality = x.nationality,
            Preferences = x.Preferences,
            InternalNotes = x.InternalNotes,
            GuestVisibleNotes = x.GuestVisibleNotes,
            IsActive = x.IsActive,
            RowVersion = Convert.ToBase64String(x.RowVersion),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
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
            var isManagerorreceptionist = data.Roles.Any(role =>
                role.IsActive &&
          (      string.Equals(
                    role.Name,
                    nameof(UserRole.Manager),
                    StringComparison.OrdinalIgnoreCase) 
                  || string.Equals(
                    role.Name,
                    nameof(UserRole.Receptionist),
                    StringComparison.OrdinalIgnoreCase)));

            if (!isManagerorreceptionist)
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
        private static object Snapshot(Guest x) => new
        { x.propertyid, 
            x.FirstName,
            x.LastName, 
            x.IdentityType,
            x.IdentityNumber,
            x.NationalId, 
            x.Phone, 
            x.Email,
            x.Address,
            x.nationality, 
            x.IsActive
        };
        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static bool CheckRowVersion(string? expected, byte[] actual)
        {
            if (string.IsNullOrWhiteSpace(expected))
                return false;

            if (!Convert.TryFromBase64String(
                    expected,
                    new byte[actual.Length],
                    out int length))
                return false;

            return Convert.ToBase64String(actual) == expected;
        }
        private static AddAuditLogDto Audit(
     User actor,
     int propertyId,
     string action,
     object oldValues,
     object newValues,
     string? reason = null,
     string? entityId = null)
        {
            return new AddAuditLogDto
            {
                userid = actor.Id,
                TargetEntity = nameof(Guest),
                TargetEntityId = entityId,
                PropertyId = propertyId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason
            };
        }
        private static ResponseStatus<T> Ok<T>(T data, int status = 200) => new(data, "Success", status);
        private static ResponseStatus<T> Fail<T>(string message, int status, List<string>? errors = null) 
            => new(message: message, errors: errors, statusCode: status);
    }
}

