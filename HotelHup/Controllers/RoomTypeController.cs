using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.RoomType;
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
    [Route("api/v1/room-types")]
    public sealed class RoomTypeController : ControllerBase
    {
        private readonly IRoomTypeService _service;
        private readonly UserManager<User> _userManager;

        public RoomTypeController(IRoomTypeService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.RoomTypes.Read)]
        public async Task<IActionResult> List([FromQuery] RoomTypeListRequest request, CancellationToken ct)
        {
            var current = await GetCurrentUserAsync();
            if (!current.Success || current.Data is null) 
            {
                return Result(current);
            }
            return Result(await _service.GetAllAsync(current.Data, request, ct));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.RoomTypes.Read)]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var current = await GetCurrentUserAsync();
            if (!current.Success || current.Data is null) return Result(current);
            return Result(await _service.GetAsync(current.Data, id, ct));
        }

        [HttpPost]
        [Authorize(Policy = Permissions.RoomTypes.Create)]
        public async Task<IActionResult> Create(CreateRoomTypeRequest request, int propertyid,CancellationToken ct)
        {
            var invalid = ValidateModelState();
            if (invalid is not null)
            {
                return invalid;
            }
            var current = await GetCurrentUserAsync();
            if (!current.Success || current.Data is null)
            {
                return Result(current);
            }
            return Result(await _service.CreateAsync(current.Data, propertyid,request, ct));
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = Permissions.RoomTypes.Update)]
        public async Task<IActionResult> Update(int id,
            [FromHeader(Name = "If-Match")] string ifMatch, 
            UpdateRoomTypeRequest request,
            CancellationToken ct)
        {
            var invalid = ValidateModelState();
            if (invalid is not null) return invalid;
            var current = await GetCurrentUserAsync();
            if (!current.Success || current.Data is null) return Result(current);
            return Result(await _service.UpdateAsync(current.Data, ifMatch, id, request, ct));
        }

        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = Permissions.RoomTypes.Deactivate)]
        public async Task<IActionResult> Deactivate(int id, [FromHeader(Name = "If-Match")] string ifMatch, CancellationToken ct)
        {
            var current = await GetCurrentUserAsync();
            if (!current.Success || current.Data is null) return Result(current);
            return Result(await _service.DeactivateAsync(current.Data, ifMatch, id, ct));
        }
        private async Task<ResponseStatus<User>> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return new ResponseStatus<User>(message: "User is not authenticated.", statusCode: 401);
            var user = await _userManager.FindByIdAsync(userId);
            return user is null
                ? new ResponseStatus<User>(message: "User not found.", statusCode: 404)
                : new ResponseStatus<User>(user);
        }

        private IActionResult? ValidateModelState()
        {
            if (ModelState.IsValid) return null;
            var errors = ModelState.Values.SelectMany(x => x.Errors)
                .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage)
                .ToList();
            return Result(new ResponseStatus<bool>(message: "Validation failed.", errors: errors, statusCode: 400));
        }
        private IActionResult Result<T>(ResponseStatus<T> response) => StatusCode(response.StatusCode, response);
    }
}