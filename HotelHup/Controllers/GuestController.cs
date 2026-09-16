using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.guest;
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
    [Route("api/v1/guests")]
    public sealed class GuestsController : ControllerBase
    {
        private readonly IGuestService _service;
        private readonly UserManager<User> _userManager;

        public GuestsController(IGuestService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Guests.Read)]
        public async Task<IActionResult> GetList([FromQuery] GuestListRequest request,int propertyid, CancellationToken ct)
        {
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.GetListAsync(actor.Data,propertyid, request, ct));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Guests.Create)]
        public async Task<IActionResult> Create(int propertyid,CreateGuestRequest request, CancellationToken ct)
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.CreateAsync(actor.Data,propertyid, request, ct));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Guests.Read)]
        public async Task<IActionResult> GetById(
     int id,
     [FromQuery] int propertyid,
     CancellationToken ct)
        {
            var actor = await GetActor();

            if (!actor.Success || actor.Data == null)
            {
                return Result(actor);
            }

            return Result(
                await _service.GetByIdAsync(
                    actor.Data,
                    propertyid,
                    id,
                    ct));
        }


        [HttpPatch("{id:int}")]
        [Authorize(Policy = Permissions.Guests.Update)]
        public async Task<IActionResult> Update(int id, int propertyid,UpdateGuestRequest request, CancellationToken ct)
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.UpdateAsync(actor.Data,propertyid, id, request, ct));
        }

        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = Permissions.Guests.Update)]
        public async Task<IActionResult> Deactivate(int id,int propertyid, DeactivateGuestRequest request, CancellationToken ct)
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.DeactivateAsync(actor.Data, propertyid,id, request, ct));
        }

        [HttpPost("{id:int}/anonymize")]
        [Authorize(Policy = Permissions.Guests.Update)]
        public async Task<IActionResult> Anonymize(int id,int propertyid, AnonymizeGuestRequest request, CancellationToken ct)
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.AnonymizeAsync(actor.Data, propertyid,id, request, ct));
        }

        [HttpGet("{id:int}/reservations")]
        [Authorize(Policy = Permissions.Guests.Read)]
        public async Task<IActionResult> ReservationHistory(int id,int propertyid, CancellationToken ct)
        {
            var actor = await GetActor();
            if (!actor.Success || actor.Data == null) return Result(actor);
            return Result(await _service.GetReservationHistoryAsync(actor.Data,propertyid, id, ct));
        }

        private async Task<ResponseStatus<User>> GetActor()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new ResponseStatus<User>("User is not authenticated.", statusCode: 401, code: "UNAUTHENTICATED");
            var user = await _userManager.FindByIdAsync(userId);
            return user == null ? new ResponseStatus<User>("User not found.", statusCode: 404, code: "USER_NOT_FOUND") : new ResponseStatus<User>(user);
        }

        private IActionResult? ValidateModelState()
        {
            if (ModelState.IsValid) return null;
            var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToList();
            return Result(new ResponseStatus<bool>("Validation failed.", errors, 400, "VALIDATION_ERROR"));
        }

        private IActionResult Result<T>(ResponseStatus<T> response) => StatusCode(response.StatusCode, response);
    }
}