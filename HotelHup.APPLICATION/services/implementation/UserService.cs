using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.aduitlog;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static HotelHup.APPLICATION.Constant.Permissions;

namespace HotelHup.APPLICATION.services.implementation
{

    public sealed class UserService : IUserService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUserRepository _repository;

        public UserService(IHttpContextAccessor httpContextAccessor,IUserRepository repository)
        {
            this.httpContextAccessor = httpContextAccessor;
            _repository = repository;
        }

        public async Task<ResponseStatus<PagedResponse<ResponseUserDto>>> GetListAsync(
            User actor, UserListRequestDto request,
            CancellationToken cancellationToken = default)
        {


            var pageNumber = Math.Max(request.pg.PageNumber, 1);
            var pageSize = Math.Clamp(request.pg.PageSize, 1, 100);
            if (actor.PropertyId.HasValue && request.PropertyId.HasValue && actor.PropertyId != request.PropertyId)
            {
                return new ResponseStatus<PagedResponse<ResponseUserDto>>(
                    message: "The requested property is outside your scope.",
                    statusCode: 403);
            }

            var effectivePropertyId = actor.PropertyId ?? request.PropertyId;
            var skip = (pageNumber - 1) * pageSize;
            var users = await _repository.GetListAsync(
                request.UserName?.Trim(),
                request.FullName?.Trim(),
                request.IsActive,
                effectivePropertyId,
                request.RoleId,
                request.CreatedFrom,
                request.CreatedTo,
                skip,
                pageSize,
                cancellationToken);
            var totalCount = await _repository.CountAsync(
                request.UserName?.Trim(),
                request.FullName?.Trim(),
                request.IsActive,
                effectivePropertyId,
                request.RoleId,
                request.CreatedFrom,
                request.CreatedTo,
                cancellationToken);

            var items = new List<ResponseUserDto>(users.Count);
            foreach (var user in users)
            {
                items.Add(await MapAsync(user, cancellationToken));
            }

            var data = new PagedResponse<ResponseUserDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            return Success(data, "Users retrieved successfully.");
        }

        public async Task<ResponseStatus<ResponseUserDto>> GetByIdAsync(
            User currentuser,string userId,
            CancellationToken cancellationToken = default)
        {
            if (userId==null||string.IsNullOrWhiteSpace(userId))
            {
                return Failer<ResponseUserDto>(message:"userid is empty",statusCode:400);
            }
            var user = await _repository.GetByIdAsync(userId, cancellationToken);
            if (currentuser is null || user is null || !await IsWithinScopeAsync(currentuser, user.PropertyId,cancellationToken))
            {
                return Failer<ResponseUserDto>(message:"User was not found.",statusCode: 404);
            }

            return Success(await MapAsync(user, cancellationToken), "User retrieved successfully.");
        }

        public async Task<ResponseStatus<ResponseUserDto>> CreateAsync(
          User user, CreateUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Failer<ResponseUserDto>(
                    "Request body is required.",
                    statusCode: 400);
            }
            var actorPermissions = await _repository.GetPermissionNamesAsync(
                user.Id,
                cancellationToken);

            if (!actorPermissions.Contains(Permissions.Users.Create))
            {
                return Failer<ResponseUserDto>(
                    "User.Create permission is required to create users.",
                    statusCode: 403);
            }
            var isGlobalAdmin = false;
            if (!user.PropertyId.HasValue)
            {
                var actorRoles = await _repository.GetRolesAsync(
                    user.Id,
                    cancellationToken);

                isGlobalAdmin = actorRoles.Any(role =>
                    string.Equals(
                        role.Name,
                        nameof(UserRole.Admin),
                        StringComparison.OrdinalIgnoreCase));

                if (!isGlobalAdmin)
                {
                    return Failer<ResponseUserDto>(
                        "A user without a PropertyId must be a Global Admin.",
                        statusCode: 403);
                }
            }

            int? propertyId = request.PropertyId;

            if (!isGlobalAdmin && !propertyId.HasValue)
            {
                return Failer<ResponseUserDto>(
                    "PropertyId is required for non-global users.",
                    statusCode: 400);
            }

            if (!isGlobalAdmin && user.PropertyId.Value != propertyId.Value)
            {
                return Failer<ResponseUserDto>(
                    "The requested property is outside your scope.",
                    statusCode: 403);
            }

            if (propertyId.HasValue)
            {
                var propertyy = await _repository.GetPropertyAsync(
                    propertyId.Value,
                    cancellationToken);
                if (propertyy is null)
                {
                    return Failer<ResponseUserDto>(
                        "The requested property was not found.",
                        statusCode: 400);
                }

                if (propertyy.Status == PropertyStatus.Inactive)
                {
                    return Failer<ResponseUserDto>(
                        "The requested property is not active.",
                        statusCode: 400);
                }
            }
            if (!isGlobalAdmin && (!user.PropertyId.HasValue || user.PropertyId.Value != propertyId))
            {
                return Failer<ResponseUserDto>(
                    "The requested property is outside your scope.",
                    statusCode: 403);
            }
            
 

            var roleValidation = await ValidateRolesAsync(
                user,
                request.RoleIds,
                cancellationToken);
            if (roleValidation.ErrorMessage is not null)
            {
                return Failer<ResponseUserDto>(
                    roleValidation.ErrorMessage,
                    roleValidation.Errors,
                    roleValidation.ErrorStatus);
            }

            var exitroles = roleValidation.Roles;
            //normalize
            var userName = request.UserName.Trim();
            var fullName = request.FullName.Trim();
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(fullName))
            {
                return Failer<ResponseUserDto>(message: "UserName and FullName are required.", statusCode: 400);
            }

            var normalizedUserName = Normalize(userName);
            if (await _repository.ExistsByNormalizedUserNameAsync(normalizedUserName, cancellationToken: cancellationToken))
            {
                return Failer<ResponseUserDto>(message: "Username is already in use.", statusCode: 409);
            }

            var userparam = new User
            {
                UserName = userName,
                NormalizedUserName = normalizedUserName,
                FullName = fullName,
                Address = NormalizeOptional(request.Address),
                PropertyId = propertyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
               
            }; var auditDto = new AddAuditLogDto
            {
                userid = user.Id,
                TargetEntity = nameof(User),
                TargetEntityId = "",
                PropertyId = user.PropertyId,
                Action = "Create",
                OldValues = null,
                NewValues = new
                {
                    user.UserName,
                    user.FullName,
                    user.PropertyId,
                    user.IsActive
                },
                Reason = null,
                CorrelationId = GetCorrelationId()
            }; var auditLog = CreateAuditLog(auditDto);
            IReadOnlyList<string> rolesname = exitroles.Select(r => r.Name).ToArray();
            var responsedatabase = await _repository.CreateWithRolesAsync(userparam,
                request.Password,
                rolesname,
                auditLog,
                cancellationToken);
            if (!responsedatabase.Succeeded)
            {
                return FailureFromIdentity<ResponseUserDto>(responsedatabase, "Unable to create user.");
            }
            var getuser = await _repository.GetByIdAsync(userparam.Id, cancellationToken);
            if (getuser==null)
            {
                return Failer<ResponseUserDto>(message:"user is not founded in database",statusCode:400);
            }
         

            return new ResponseStatus<ResponseUserDto>(
                await MapAsync(getuser, cancellationToken), "User created successfully.", 201);
        }

        public async Task<ResponseStatus<ResponseUserDto>> UpdateAsync(
            User actor,string userId,
            UpdateUserRequestDto request,
            CancellationToken cancellationToken = default)
        {
         //   var actor=await _repository.GetByIdAsync(request.actorid, cancellationToken);
            if (actor==null||!actor.IsActive)
            {
                return Failer<ResponseUserDto>(message: "actor is not found", statusCode: 400);

            }
            var user = await _repository.GetByIdAsync(userId, cancellationToken);
            //check user is active 
            if (user ==null|| !user.IsActive)
            {
                return Failer<ResponseUserDto>(message:"user is not found",statusCode:400);
            }
            if (actor is null ||!await IsWithinScopeAsync(actor, user.PropertyId.Value, cancellationToken)) 
            {
                return Failer<ResponseUserDto>(message:"User was not found.",statusCode:404);
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changed = false;

            if (request.FullName is not null)
            {
                var fullName = request.FullName.Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return Failer<ResponseUserDto>(message:"FullName cannot be empty.",statusCode: 400);
                }

                if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
                {
                    oldValues[nameof(user.FullName)] = user.FullName;
                    newValues[nameof(user.FullName)] = fullName;
                    user.FullName = fullName;
                    changed = true;
                }
            }

            if (request.Address is not null)
            {
                var address = NormalizeOptional(request.Address);
                if (!string.Equals(user.Address, address, StringComparison.Ordinal))
                {
                    oldValues[nameof(user.Address)] = user.Address;
                    newValues[nameof(user.Address)] = address;
                    user.Address = address;
                    changed = true;
                }
            }

            if (!changed)
            {
                return Success(await MapAsync(user, cancellationToken), "No user fields changed.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            var addaduitdto = new AddAuditLogDto
            {
                Action = "update",
                userid = actor.Id,
                PropertyId = user.PropertyId,
                Reason = null,
                TargetEntity = nameof(User),
                TargetEntityId = user.Id,
                OldValues = oldValues,
                NewValues = newValues,
                CorrelationId = GetCorrelationId()
            };
            var audit = CreateAuditLog(addaduitdto);
            var updateResult = await _repository.UpdateWithAuditAsync(user, audit, cancellationToken);
            if (!updateResult.Succeeded)
            {
                return FailureFromIdentity<ResponseUserDto>(updateResult, "Unable to update user.");
            }

            var updated = await _repository.GetByIdAsync(user.Id, cancellationToken) ?? user;
            return Success(await MapAsync(updated, cancellationToken), "User updated successfully.");
        }

        public async Task<ResponseStatus<ResponseUserDto>> ActivateAsync(
            User currentuser,string userId,
            CancellationToken cancellationToken = default)
        {

             var user = await _repository.GetuserByIdAsync(userId, cancellationToken);
            if (currentuser is null || user is null || !await IsWithinScopeAsync(currentuser, user.PropertyId,cancellationToken))
            {
                return Failer<ResponseUserDto>(message:"User was not found.",statusCode: 404);
            }
            if (user.IsActive)
            {
                return Success(await MapAsync(user, cancellationToken), "User is already active.");
            }


            if (user.PropertyId.HasValue)
            {
                var property = await _repository.GetPropertyAsync(user.PropertyId.Value, cancellationToken);
                if (property is null)
                {
                    return Failer<ResponseUserDto>(message:"Property was not found.",statusCode: 404);
                }

                if (property.Status != PropertyStatus.Active)
                {
                    return Failer<ResponseUserDto>(message:"Users can only be activated in an active property.",statusCode: 422);
                }
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            var auditlogdto = new AddAuditLogDto 
            {
                Action="activate",
                CorrelationId=GetCorrelationId(),
                NewValues = new { IsActive = true },
                OldValues = new { IsActive = false },
                PropertyId=user.PropertyId,
                Reason=null,
                userid=currentuser.Id,
                TargetEntity=nameof(User),
                TargetEntityId=user.Id
            };
            var audit = CreateAuditLog(auditlogdto);
            var updateResult = await _repository.UpdateWithAuditAsync(user, audit, cancellationToken);
            if (!updateResult.Succeeded)
            {
                return FailureFromIdentity<ResponseUserDto>(updateResult, "Unable to activate user.");
            }

            var activated = await _repository.GetByIdAsync(user.Id, cancellationToken) ?? user;
            return Success(await MapAsync(activated, cancellationToken), "User activated successfully.");
        }

        public async Task<ResponseStatus<ResponseUserDto>> DeactivateAsync(
            User currentuser,string userId,
            
            CancellationToken cancellationToken = default)
        {
            

           
            var user = await _repository.GetuserByIdAsync(userId, cancellationToken);
            if (currentuser is null || user is null || !await IsWithinScopeAsync(currentuser, user.PropertyId,cancellationToken))
            {
                return Failer<ResponseUserDto>(message:"User was not found.",statusCode: 404);
            }

            if (!user.IsActive)
            {
                return Success(await MapAsync(user, cancellationToken), "User is already inactive.");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            var auditlogdto = new AddAuditLogDto
            {
                Action = "deactivate",
                CorrelationId = GetCorrelationId(),
                NewValues = new { IsActive = true },
                OldValues = new { IsActive = false },
                PropertyId = user.PropertyId,
                Reason = null,
                userid = currentuser.Id,
                TargetEntity = nameof(User),
                TargetEntityId = user.Id
            };
            var audit = CreateAuditLog(auditlogdto);
            var updateResult = await _repository.UpdateWithAuditAsync(user, audit, cancellationToken);
            if (!updateResult.Succeeded)
            {
                return FailureFromIdentity<ResponseUserDto>(updateResult, "Unable to deactivate user.");
            }

            var deactivated = await _repository.GetByIdAsync(user.Id, cancellationToken) ?? user;
            return Success(await MapAsync(deactivated, cancellationToken), "User deactivated successfully.");
        }

        public async Task<ResponseStatus<IReadOnlyList<ResponseRoleDto>>> GetRolesAsync(
            User currentuser,string userId,
            CancellationToken cancellationToken = default)
        {
           

             var user = await _repository.GetByIdAsync(userId, cancellationToken);
            if (currentuser is null || user is null || !await IsWithinScopeAsync(currentuser, user.PropertyId,cancellationToken))
            {
                return Failer<IReadOnlyList<ResponseRoleDto>>(message:"User was not found.",statusCode: 404);
            }

            var roles = await _repository.GetRolesAsync(user.Id, cancellationToken);
            var data = roles.Select(MapRole).ToList();
            return Success<IReadOnlyList<ResponseRoleDto>>(data, "User roles retrieved successfully.");
        }

        public async Task<ResponseStatus<ResponseUserDto>> ReplaceRolesAsync(
      User actor, string userId,
       ReplaceUserRolesRequestDto request,
       CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Failer<ResponseUserDto>(
                    message: "Request body is required.",
                    statusCode: 400);
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Failer<ResponseUserDto>(
                    message: "User ID is required.",
                    statusCode: 400);
            }

 

           

            var user = await _repository.GetByIdAsync(
                userId,
                cancellationToken);

            if (actor is null ||
                !actor.IsActive ||
                user is null ||
                !user.IsActive ||
                !await IsWithinScopeAsync(
                    actor,
                    user.PropertyId,
                    cancellationToken))
            {
                return Failer<ResponseUserDto>(
                    message: "User was not found.",
                    statusCode: 404);
            }

            var actorPermissions = await _repository.GetPermissionNamesAsync(
                actor.Id,
                cancellationToken);

            if (!actorPermissions.Contains(Permissions.Users.AssignRole))
            {
                return Failer<ResponseUserDto>(
                    message: "User.AssignRole permission is required to replace roles.",
                    statusCode: 403);
            }

            var requestedRoleIds = request.RoleIds?.ToList()
                                   ?? new List<string>();

            var rolesResult = await ValidateRolesAsync(
                actor,
                requestedRoleIds,
                cancellationToken);

            if (rolesResult.ErrorMessage is not null)
            {
                return Failer<ResponseUserDto>(
                    rolesResult.ErrorMessage,
                    rolesResult.Errors,
                    rolesResult.ErrorStatus);
            }

            var oldRoles = await _repository.GetRolesAsync(
                user.Id,
                cancellationToken);

            var newRoleNames = rolesResult.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Name))
                .Select(role => role.Name!)
                .ToArray();

            var auditLogDto = new AddAuditLogDto
            {
                userid = actor.Id,
                TargetEntity = nameof(User),
                TargetEntityId = user.Id,
                PropertyId = user.PropertyId,
                Action = "ReplaceRoles",
                OldValues = oldRoles
                    .Select(MapRole)
                    .ToList(),
                NewValues = rolesResult.Roles
                    .Select(MapRole)
                    .ToList(),
                Reason = null,
                CorrelationId = GetCorrelationId()
            };

            var auditLog = CreateAuditLog(auditLogDto);

            var replaceResult = await _repository.ReplaceRolesAsync(
                user,
                newRoleNames,
                auditLog,
                cancellationToken);

            if (!replaceResult.Succeeded)
            {
                return FailureFromIdentity<ResponseUserDto>(
                    replaceResult,
                    "Unable to replace user roles.");
            }

            var updatedUser = await _repository.GetByIdAsync(
                user.Id,
                cancellationToken) ?? user;

            return Success(
                await MapAsync(updatedUser, cancellationToken),
                "User roles replaced successfully.");
        }

        public async Task<ResponseStatus<bool>>checkpermissionandstateuser(
            string userid,
            CancellationToken cancellationToken,
            string permission)
        {
            //state
            var user = await _repository.GetByIdAsync(userid, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return new ResponseStatus<bool>(statusCode: 400, message: "this user is not active");
            }
            //permission
            var permissions = await _repository.GetPermissionNamesAsync(userid, cancellationToken);
            if (!permissions.Contains(permission.Trim()))
            {
                return new ResponseStatus<bool>(
                        statusCode: 403,
                        message: "User does not have this permission"
                );
            }
            return new ResponseStatus<bool>(
                data: true,
                message: "User is active and has permission",
                statusCode: 200
            );
        }
        private ResponseStatus<T> Failer<T>(
            string message,
            IEnumerable<string>? errors = null,
            int statusCode = 400)
        {
            return new ResponseStatus<T>(
                message,
                errors?.ToList(),
                statusCode);
        }
        private ResponseStatus<T> Success<T>(
            T data,
            string message = "",
            int statusCode = 200)
        {
            return new ResponseStatus<T>(
                data,
                message,
                statusCode);
        }
        private async Task<RoleValidationResult> ValidateRolesAsync(
    User actor,
    IEnumerable<string>? roleIds,
    CancellationToken cancellationToken)
        {
            var requestedRoleIds = roleIds?.ToList() ?? new List<string>();
            if (requestedRoleIds.Count == 0)
            {
                return RoleValidationResult.Success(Array.Empty<Role>());
            }

            if (requestedRoleIds.Any(string.IsNullOrWhiteSpace))
            {
                return RoleValidationResult.Failure("Role IDs cannot be empty.");
            }

            if (requestedRoleIds.Distinct(StringComparer.Ordinal).Count() != requestedRoleIds.Count)
            {
                return RoleValidationResult.Failure("Duplicated role IDs are not allowed.");
            }

            var roles = await _repository.GetRolesByIdsAsync(
                requestedRoleIds,
                cancellationToken);
            var requestedRoleIdSet = requestedRoleIds.ToHashSet(StringComparer.Ordinal);
            if (roles.Count != requestedRoleIdSet.Count ||
                roles.Any(role => !requestedRoleIdSet.Contains(role.Id)))
            {
                return RoleValidationResult.Failure("There are role IDs that were not found.");
            }

            var inactiveRoleErrors = roles
                .Where(role => !role.IsActive)
                .Select(role => $"{role.Id} is not active")
                .ToList();
            if (inactiveRoleErrors.Count > 0)
            {
                return RoleValidationResult.Failure(
                    "There are inactive role IDs.",
                    inactiveRoleErrors);
            }

            var actorPermissions = await _repository.GetPermissionNamesAsync(
                actor.Id,
                cancellationToken);
            if (!actorPermissions.Contains(Permissions.Users.AssignRole))
            {
                return RoleValidationResult.Failure(
                    "User.AssignRole permission is required to assign roles.");
            }

            var requestedPermissions = await _repository
                .GetPermissionNamesByRoleIdsAsync(
                    requestedRoleIds,
                    cancellationToken);
            if (requestedPermissions.Any(permission => !actorPermissions.Contains(permission)))
            {
                return RoleValidationResult.Failure(
                    "The requested roles would escalate privileges.");
            }

            return RoleValidationResult.Success(roles);
        }

        private sealed class RoleValidationResult
        {
            private RoleValidationResult(
                IReadOnlyList<Role> roles,
                string? errorMessage = null,
                IEnumerable<string>? errors = null,
                int errorStatus = 400)
            {
                Roles = roles;
                ErrorMessage = errorMessage;
                Errors = errors?.ToList() ?? new List<string>();
                ErrorStatus = errorStatus;
            }

            public IReadOnlyList<Role> Roles { get; }

            public string? ErrorMessage { get; }

            public IReadOnlyList<string> Errors { get; }

            public int ErrorStatus { get; }

            public static RoleValidationResult Success(
                IReadOnlyList<Role> roles)
            {
                return new RoleValidationResult(roles);
            }

            public static RoleValidationResult Failure(
                string message,
                IEnumerable<string>? errors = null,
                int status = 400)
            {
                return new RoleValidationResult(
                    Array.Empty<Role>(),
                    message,
                    errors,
                    status);
            }
        }
 

        private async Task<ResponseUserDto> MapAsync(

                 User user,
                 CancellationToken cancellationToken)
        {
            var roles = await _repository.GetRolesAsync(
                user.Id,
                cancellationToken);

            return new ResponseUserDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Address = user.Address,
                PropertyId = user.PropertyId,
                PropertyName = user.Property?.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles
                    .Select(role => new ResponseRoleDto
                    {
                        RoleId = role.Id,
                        RoleName = role.Name ?? string.Empty
                    })
                    .ToList()
            };
        }
        private static string Normalize(string value)
        {
            return value.Trim().ToUpperInvariant();
        }
        private static AuditLog CreateAuditLog(AddAuditLogDto auditDto)
        {
            if (auditDto is null)
            {
                throw new ArgumentNullException(nameof(auditDto));
            }

            return new AuditLog
            {
                UserId = auditDto.userid,
                PropertyId = auditDto.PropertyId,
                EntityName = auditDto.TargetEntity,
                EntityId = auditDto.TargetEntityId,
                Action = auditDto.Action,
                OldValues = auditDto.OldValues is null
                    ? null
                    : JsonSerializer.Serialize(auditDto.OldValues),
                NewValues = auditDto.NewValues is null
                    ? null
                    : JsonSerializer.Serialize(auditDto.NewValues),
                Reason = auditDto.Reason,
                CorrelationId = auditDto.CorrelationId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = auditDto.userid
            };
        }
        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToUpperInvariant();
        }
        private static ResponseStatus<T> FailureFromIdentity<T>(
    IdentityResult result,
    string message)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            var statuscode = 0;
            if (result.Errors.Any(e => e.Code == "DuplicateUserName" || e.Code == "ConcurrencyFailure"))
            {
                statuscode = 409;
            }
            else
            {
                statuscode = 400;
            }
            return new ResponseStatus<T>(message,errors, statuscode);
        }
        private async Task<bool> IsWithinScopeAsync(
      User actor,
      int? targetPropertyId,
      CancellationToken cancellationToken)
        {
             if (actor is null || !actor.IsActive)
             {
                return false;
             }

             if (actor.PropertyId.HasValue)
             {
                return targetPropertyId.HasValue &&
                       actor.PropertyId.Value == targetPropertyId.Value;
             }

         
            var actorRoles = await _repository.GetRolesAsync(
                actor.Id,
                cancellationToken);

            var isGlobalAdmin = actorRoles.Any(role =>
                role.IsActive &&
                string.Equals(
                    role.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase));

            if (!isGlobalAdmin)
            {
                return false;
            }

             return true;
        }

        private static ResponseRoleDto MapRole(Role role)
        {
            return new ResponseRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                isactive = role.IsActive,
            };
        }
        private string GetCorrelationId()
        {
            var httpContext = httpContextAccessor.HttpContext;
           
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
