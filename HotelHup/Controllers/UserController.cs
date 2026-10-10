using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.role;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HotelHup.API.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> userManager;

        public UserController(IUserService userService, UserManager<User> userManager)
        {
            _userService = userService;
            this.userManager = userManager;
        }
        //check current user login or not
        [HttpGet]
        [Authorize(Policy = Permissions.Users.Read)]
        public async Task<IActionResult> GetList(
            [FromQuery] UserListRequestDto request,
            CancellationToken cancellationToken)
        {
            // Business rule: the endpoint requires Users.Read and the service applies the actor's Property scope.
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                  .Values
                    .SelectMany(x => x.Errors)
                      .Select(x => x.ErrorMessage)
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToList();

                var results = new ResponseStatus<UserListRequestDto>(
                    message: "Invalid request",
                    errors: errors,
                    statusCode: 400
                );
                return ToActionResult(results);
            }

            var checkauth = await Authorized(cancellationToken, Permissions.Users.Read);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            var result = await _userService.GetListAsync(checkauth.Data, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("{userId}")]
        [Authorize(Policy = Permissions.Users.Read)]

        public async Task<IActionResult> GetById(string userId, CancellationToken cancellationToken)
        {
            var checkauth = await Authorized(cancellationToken, Permissions.Users.Read);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            var result = await _userService.GetByIdAsync(checkauth.Data, userId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("{userId}/roles")]
        [Authorize(Policy = Permissions.Users.Read)]
        public async Task<IActionResult> GetRoles(string userId, CancellationToken cancellationToken)
        {
            var checkauth = await Authorized(cancellationToken, Permissions.Users.Read);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            var result = await _userService.GetRolesAsync(checkauth.Data, userId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Create)]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            // Business rule: authentication, activation, and Users.Create are checked before user creation reaches the service.
            // The service remains responsible for PropertyId, role, privilege-escalation, and password rules.
            //check request
            if (request == null)
            {
                return ToActionResult(new ResponseStatus<ResponseUserDto>(statusCode: 400, message: "request is null"));
            }
            //modelvalidation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                  .Values
                    .SelectMany(x => x.Errors)
                      .Select(x => x.ErrorMessage)
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToList();

                var results = new ResponseStatus<UserListRequestDto>(
                    message: "Invalid request",
                    errors: errors,
                    statusCode: 400
                );
                return ToActionResult(results);
            }
            //check current user
            var user = await getcurrentuserlogeined();
            //check permission and statues user 
            if (user.Data != null)
            {
                var permissionstatus = await _userService.checkpermissionandstateuser(user.Data.Id, cancellationToken, Permissions.Users.Create);
                if (permissionstatus.StatusCode != 200)
                {
                    return ToActionResult(permissionstatus);
                }
            }
            //check property statues
            var result = await _userService.CreateAsync(user.Data, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPatch("{userId}")]
        [Authorize(Policy = Permissions.Users.Update)]
        public async Task<IActionResult> Update(
            string userId,
            [FromBody] UpdateUserRequestDto request,
            CancellationToken cancellationToken)
        {
            // Business rule: only an active actor with Users.Update can update another user; the service checks target scope and audit.
            //check request
            if (request == null)
            {
                return ToActionResult(new ResponseStatus<ResponseUserDto>(statusCode: 400, message: "request is null"));
            }
            //modelvalidation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                  .Values
                    .SelectMany(x => x.Errors)
                      .Select(x => x.ErrorMessage)
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToList();

                var results = new ResponseStatus<UserListRequestDto>(
                    message: "Invalid request",
                    errors: errors,
                    statusCode: 400
                );
                return ToActionResult(results);
            }
            //check current user
            var user = await getcurrentuserlogeined();
            if (user.Data == null)
            {
                return ToActionResult(user);
            }
            //check permission and statues user 
            if (user.Data != null)
            {
                var permissionstatus = await _userService.checkpermissionandstateuser(user.Data.Id, cancellationToken, Permissions.Users.Update);
                if (permissionstatus.StatusCode != 200)
                {
                    return ToActionResult(permissionstatus);
                }
            }
            var result = await _userService.UpdateAsync(user.Data, userId, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{userId}/activate")]
        [Authorize(Policy = Permissions.Users.Activate)]
        public async Task<IActionResult> Activate(string userId, CancellationToken cancellationToken)
        {

            var checkauth = await Authorized(cancellationToken, Permissions.Users.Activate);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            var result = await _userService.ActivateAsync(checkauth.Data, userId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{userId}/deactivate")]
        [Authorize(Policy = Permissions.Users.Deactivate)]
        public async Task<IActionResult> Deactivate(
            string userId,
            CancellationToken cancellationToken)
        {
            var checkauth = await Authorized(cancellationToken, Permissions.Users.Deactivate);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            var result = await _userService.DeactivateAsync(checkauth.Data, userId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPut("{userId}/roles")]
        [Authorize(Policy = Permissions.Users.AssignRole)]
        public async Task<IActionResult> ReplaceRoles(
            string userId,
            [FromBody] ReplaceUserRolesRequestDto request,
            CancellationToken cancellationToken)
        {
            // Business rule: role replacement is privileged, scope-limited, and audited as one atomic repository operation.
            var checkauth = await Authorized(cancellationToken, Permissions.Users.AssignRole);
            if (!checkauth.Success || checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }
            // request.ActorId = checkauth.Data.Id;
            var result = await _userService.ReplaceRolesAsync(checkauth.Data, userId, request, cancellationToken);
            return ToActionResult(result);
        }
        [HttpGet("roles")]
        [Authorize(Policy = Permissions.Users.Read)]
        public async Task<IActionResult> GetAllRoles(
    [FromQuery] RoleListRequestDto request,
    CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .Where(message =>
                        !string.IsNullOrWhiteSpace(message))
                    .ToList();

                var invalidResponse =
                    new ResponseStatus<PagedResponse<RoleListResponseDto>>(
                        message: "Invalid request.",
                        errors: errors,
                        statusCode: 400);

                return ToActionResult(invalidResponse);
            }

            var checkauth = await Authorized(
                cancellationToken,
                Permissions.Users.Read);

            if (!checkauth.Success ||
                checkauth.Data == null)
            {
                return ToActionResult(checkauth);
            }

            var result = await _userService.GetRolesListAsync(
                checkauth.Data,
                request,
                cancellationToken);

            return ToActionResult(result);
        }

        private IActionResult ToActionResult<T>(ResponseStatus<T> result)
        {
            return StatusCode(result.StatusCode, result);
        }
        private async Task<ResponseStatus<User>> Authorized(CancellationToken cancellationToken, string permission)
        {
            // Business rule: every protected action re-loads the actor and checks the requested permission server-side.
            // UI visibility and the [Authorize] attribute alone are not sufficient authorization.
            //check current user
            var user = await getcurrentuserlogeined();
            if (user.Success == false)
            {
                return new ResponseStatus<User>(message: "user is not authentaction", statusCode: 404);
            }
            //check permission and statues user 
            if (user.Data != null)
            {
                var permissionstatus = await _userService.checkpermissionandstateuser(user.Data.Id, cancellationToken, permission);
                if (permissionstatus.StatusCode != 200)
                {
                    return new ResponseStatus<User>(message: "must be has read permission", statusCode: 400);
                }
            }
            return new ResponseStatus<User>(data: user.Data ?? null);
        }
        private async Task<ResponseStatus<User>> getcurrentuserlogeined()
        {
            // Business rule: an inactive user cannot continue user-management operations even if an old JWT is still valid.
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userid == null)
            {
                return new ResponseStatus<User>(statusCode: 401, message: "user is not authenticated");
            }
            var user = await userManager.FindByIdAsync(userid);
            if (user == null)
            {
                return new ResponseStatus<User>(
                    "User not found",
                    statusCode: 404
                );
            }
            if (!user.IsActive)
            {
                return new ResponseStatus<User>(statusCode: 403, message: "user is not a");

            }
            return new ResponseStatus<User>(user);
        }

    }
}
