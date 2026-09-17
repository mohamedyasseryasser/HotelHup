using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.RatePlane;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelHup.API.Controllers
{

    [ApiController]
    [Authorize]
    [Route("api/v1/rate-plans")]
    public sealed class RatePlanController : ControllerBase
    {
        private readonly IRatePlanService _service;
        private readonly UserManager<User> _userManager;
        public RatePlanController(IRatePlanService service, UserManager<User> userManager) { _service = service; _userManager = userManager; }

        [HttpGet]
        [Authorize(Policy = Permissions.RatePlans.Read)]
        public async Task<IActionResult> List([FromQuery]int propertyid,CancellationToken ct)
        { 
            var u = await Current();
            if (!u.Success || u.Data is null) 
                return Result(u);
            return Result(await _service.GetAllAsync(u.Data,propertyid, ct)); }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.RatePlans.Read)]
        public async Task<IActionResult> Get(int id, CancellationToken ct) 
        { var u = await Current();
            if (!u.Success || u.Data is null) 
                return Result(u); 
            return Result(await _service.GetAsync(u.Data, id, ct)); 
        }

        [HttpPost]
        [Authorize(Policy = Permissions.RatePlans.Create)]
        public async Task<IActionResult> Create
            (
         [FromQuery] int propertyid,
    [FromQuery] int roomtypeid,
    [FromBody] CreateRatePlanRequest request,
    CancellationToken ct)
        {
            //modelstate validation
            var validationResult = ValidateModelState();
            if (validationResult is not null)
                return validationResult;

            //authentaction check
            var CurrentUserLogin = await Current();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            return Result(await _service.CreateAsync(CurrentUserLogin.Data,propertyid,roomtypeid, request, ct));
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = Permissions.RatePlans.Update)]
        public async Task<IActionResult> Update
            (
 int id,
    [FromHeader(Name = "If-Match")] string? ifMatch,
    [FromBody] UpdateRatePlanRequest request,
    CancellationToken ct)
        {
            //modelstate validation
            var validationResult = ValidateModelState();
            if (validationResult is not null)
                return validationResult;

            //authentaction check
            var CurrentUserLogin = await Current();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            return Result(await _service.UpdateAsync(CurrentUserLogin.Data, ifMatch, id, request, ct));
        }

        [HttpGet("{id:int}/versions")]
        [Authorize(Policy = Permissions.RatePlans.Read)]
        public async Task<IActionResult> Versions(int id, CancellationToken ct)
        { var u = await Current(); 
            if (!u.Success || u.Data is null) 
                return Result(u);
            return Result(await _service.GetVersionsAsync(u.Data, id, ct)); }

        [HttpGet("{id:int}/versions/{versionNumber:int}")]
        [Authorize(Policy = Permissions.RatePlans.Read)]
        public async Task<IActionResult> Version(int id, int versionNumber, CancellationToken ct) { var u = await Current(); if (!u.Success || u.Data is null) return Result(u); return Result(await _service.GetVersionAsync(u.Data, id, versionNumber, ct)); }

        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = Permissions.RatePlans.Deactivate)]
        public async Task<IActionResult> Deactivate(
  int id,
    [FromHeader(Name = "If-Match")] string? ifMatch,
    CancellationToken ct)
        { var u = await Current(); if (!u.Success || u.Data is null) return Result(u); return Result(await _service.DeactivateAsync(u.Data, ifMatch, id, ct)); }

        private async Task<ResponseStatus<User>> Current()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userid == null)
            {
                return new ResponseStatus<User>(statusCode: 401, message: "user is not authenticated");
            }
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null)
            {
                return new ResponseStatus<User>(
                    "User not found",
                    statusCode: 404
                );
            }
            return new ResponseStatus<User>(user);
        }
        private IActionResult? ValidateModelState()
        {
            if (ModelState.IsValid)
                return null;

            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Invalid value."
                        : e.ErrorMessage)
                .ToList();

            var response = new ResponseStatus<bool>(
                message: "Validation failed.",
                errors: errors,
                statusCode: 400);

            return Result(response);
        }


        private IActionResult Result<T>(ResponseStatus<T> response)
        {
            return StatusCode(response.StatusCode, response);
        }

    }
}
