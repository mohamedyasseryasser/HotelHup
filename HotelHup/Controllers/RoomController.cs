using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.room;
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
    [Route("api/v1/rooms")]
    public sealed class RoomController : ControllerBase
    {
        private readonly IRoomService _service;
        private readonly UserManager<User> _users;
        public RoomController(IRoomService  service, UserManager<User> users) { _service = service; _users = users; }

        [HttpGet]
        [Authorize(Policy = Permissions.Rooms.Read)]
        public async Task<IActionResult> List([FromQuery] RoomListRequest request, CancellationToken ct)
        {
            var current = await CurrentUserAsync();
            {
                if (!current.Success || current.Data is null) return Result(current);
            }
            return Result(await _service.GetListAsync(current.Data, request, ct));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Rooms.Read)]
        public async Task<IActionResult> Get(int id, [FromQuery] int propertyId, CancellationToken ct)
        {
            var current = await CurrentUserAsync();
            {
                if (!current.Success || current.Data is null) return Result(current);
            }
            return Result(await _service.GetAsync(current.Data, propertyId, id, ct));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Rooms.Create)]
        public async Task<IActionResult> Create([FromQuery] int propertyId, CreateRoomRequest request, CancellationToken ct)
        {
            var invalid = ValidateModelState();
            {
                if (invalid is not null) return invalid;
            }
            var current = await CurrentUserAsync();
            {
                if (!current.Success || current.Data is null) return Result(current);
            }
            return Result(await _service.CreateAsync(current.Data, propertyId, request, ct));
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = Permissions.Rooms.Update)]
        public async Task<IActionResult> Update(int id, [FromQuery] int propertyId, UpdateRoomRequest request, CancellationToken ct)
        {
            var invalid = ValidateModelState();
            {
                if (invalid is not null) return invalid;
            }
            var current = await CurrentUserAsync();
            {
                if (!current.Success || current.Data is null) return Result(current);
            }
            return Result(await _service.UpdateAsync(current.Data, propertyId, id, request, ct));
        }

        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = Permissions.Rooms.Update)]
        public async Task<IActionResult> Deactivate(int id, [FromQuery] int propertyId, DeactivateRoomRequest request, CancellationToken ct)
        {
            var invalid = ValidateModelState(); if (invalid is not null) return invalid;
            var current = await CurrentUserAsync(); if (!current.Success || current.Data is null) return Result(current);
            return Result(await _service.DeactivateAsync(current.Data, propertyId, id, request, ct));
        }

        private async Task<ResponseStatus<User>> CurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return new ResponseStatus<User>(message: "User is not authenticated.", statusCode: 401);
            var user = await _users.FindByIdAsync(userId);
            return user is null ? new ResponseStatus<User>(message: "User not found.", statusCode: 404) : new ResponseStatus<User>(user);
        }
        private IActionResult? ValidateModelState()
        {
            if (ModelState.IsValid) return null;
            var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToList();
            return Result(new ResponseStatus<bool>(message: "Validation failed.", errors: errors, statusCode: 400));
        }
        private IActionResult Result<T>(ResponseStatus<T> response) => StatusCode(response.StatusCode, response);
    }

}
