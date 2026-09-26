using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.folio;
using HotelHup.APPLICATION.DTO.General;
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
    [Route("api/v1/folios")]
    public sealed class FolioController : ControllerBase
    {
        private readonly UserManager<User> _users;
        private readonly IFolioService _service;

        public FolioController(
            UserManager<User> users,
            IFolioService service)
        {
            _users = users;
            _service = service;
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Folios.Read)]
        public async Task<IActionResult> Get(
            int id,
            CancellationToken ct)
        {
            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.GetAsync(
                    actor.Data,
                    id,
                    ct));
        }

        [HttpPost("{id:int}/items")]
        [Authorize(Policy = Permissions.Folios.AddCharge)]
        public async Task<IActionResult> AddItem(
            int id,
            [FromBody] AddFolioItemRequestDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationError();

            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.AddItemAsync(
                    actor.Data,
                    id,
                    request,
                    ct));
        }

        [HttpPost("{id:int}/items/{itemId:int}/void")]
        [Authorize(Policy = Permissions.Folios.Update)]
        public async Task<IActionResult> VoidItem(
            int id,
            int itemId,
            [FromBody] VoidFolioItemRequestDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationError();

            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.VoidItemAsync(
                    actor.Data,
                    id,
                    itemId,
                    request,
                    ct));
        }

        [HttpPost("{id:int}/reopen")]
        [Authorize(Policy = Permissions.Folios.Reopen)]
        public async Task<IActionResult> Reopen(
            int id,
            [FromBody] ReopenFolioRequestDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationError();

            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.ReopenAsync(
                    actor.Data,
                    id,
                    request,
                    ct));
        }

        [HttpPost("{id:int}/close")]
        [Authorize(Policy = Permissions.Folios.Close)]
        public async Task<IActionResult> Close(
            int id,
            [FromBody] CloseFolioRequestDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationError();

            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.CloseAsync(
                    actor.Data,
                    id,
                    request,
                    ct));
        }

        [HttpGet("{id:int}/items")]
        [Authorize(Policy = Permissions.Folios.Read)]
        public async Task<IActionResult> Items(
            int id,
            [FromQuery] FolioItemPageRequest request,
            CancellationToken ct)
        {
            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.GetItemsAsync(
                    actor.Data,
                    id,
                    request,
                    ct));
        }

        [HttpGet("{id:int}/summary")]
        [Authorize(Policy = Permissions.Folios.Read)]
        public async Task<IActionResult> Summary(
            int id,
            CancellationToken ct)
        {
            var actor = await Actor();

            if (!actor.Success || actor.Data is null)
                return Result(actor);

            return Result(
                await _service.GetSummaryAsync(
                    actor.Data,
                    id,
                    ct));
        }

        private async Task<ResponseStatus<User>> Actor()
        {
            var id =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(id))
            {
                return new(
                    message: "User is not authenticated.",
                    statusCode: 401,
                    code: "UNAUTHENTICATED");
            }

            var user = await _users.FindByIdAsync(id);

            if (user is null)
            {
                return new(
                    message: "User not found.",
                    statusCode: 404,
                    code: "USER_NOT_FOUND");
            }

            if (!user.IsActive)
            {
                return new(
                    message: "User is inactive.",
                    statusCode: 403,
                    code: "USER_INACTIVE");
            }

            return new(user);
        }

        private IActionResult ValidationError()
        {
            return Result(
                new ResponseStatus<bool>(
                    message: "Validation failed.",
                    errors: ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e =>
                            string.IsNullOrWhiteSpace(e.ErrorMessage)
                                ? "Invalid value."
                                : e.ErrorMessage)
                        .ToList(),
                    statusCode: 400,
                    code: "VALIDATION_ERROR"));
        }

        private IActionResult Result<T>(
            ResponseStatus<T> response)
        {
            return StatusCode(
                response.StatusCode,
                response);
        }
    }
}