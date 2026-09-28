using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.housekeeping;
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
    [Route("api/v1/housekeeping")]
    public sealed class HousekeepingController : ControllerBase
    {
        private readonly IHousekeepingService _service;
        private readonly UserManager<User> _users;
        public HousekeepingController(IHousekeepingService service, UserManager<User> users) { _service = service; _users = users; }

        [HttpGet("tasks")]
        [Authorize(Policy = Permissions.Housekeeping.Read)]
        public async Task<IActionResult> GetTasks([FromQuery] HousekeepingTaskListRequest request, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.GetTasksAsync(actor.Data, request, ct));
        }

        [HttpGet("tasks/{id:int}")]
        [Authorize(Policy = Permissions.Housekeeping.Read)]
        public async Task<IActionResult> GetTask(int id, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.GetTaskByIdAsync(actor.Data, id, ct));
        }

        [HttpPost("tasks")]
        [Authorize(Policy = Permissions.Housekeeping.Assign)]
        public async Task<IActionResult> 
            CreateTask(
            [FromBody] CreateHousekeepingTaskRequest request, 
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError();
            }
            var actor = await Actor();
            if (!actor.Success || actor.Data is null)
            {
                return Result(actor);
            }
            return Result(await _service.CreateTaskAsync(actor.Data, request, ct));
        }

        [HttpPost("tasks/{id:int}/assign")]
        [Authorize(Policy = Permissions.Housekeeping.Assign)]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignHousekeepingTaskRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationError();
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.AssignTaskAsync(actor.Data, id, request, ct));
        }

        [HttpPost("tasks/{id:int}/start")]
        [Authorize(Policy = Permissions.Housekeeping.Start)]
        public async Task<IActionResult> Start(int id, [FromBody] HousekeepingActionRequest? request, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.StartCleaningAsync(actor.Data, id, request ?? new(), ct));
        }

        [HttpPost("tasks/{id:int}/complete")]
        [Authorize(Policy = Permissions.Housekeeping.Complete)]
        public async Task<IActionResult> Complete(int id, [FromBody] HousekeepingActionRequest? request, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.CompleteCleaningAsync(actor.Data, id, request ?? new(), ct));
        }

        [HttpPost("/api/v1/rooms/{id:int}/cleaning-complete")]
        [Authorize(Policy = Permissions.Housekeeping.Complete)]
        public async Task<IActionResult> CompleteRoomCleaning(int id, [FromBody] HousekeepingActionRequest? request, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.CompleteCleaningByRoomAsync(actor.Data, id, request ?? new(), ct));
        }

        [HttpPost("tasks/{id:int}/inspect")]
        [Authorize(Policy = Permissions.Housekeeping.Complete)]
        public async Task<IActionResult> Inspect(int id, [FromBody] HousekeepingActionRequest? request, CancellationToken ct)
        {
            var actor = await Actor(); if (!actor.Success || actor.Data is null) return Result(actor);
            return Result(await _service.InspectRoomAsync(actor.Data, id, request ?? new(), ct));
        }

        private async Task<ResponseStatus<User>> Actor()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(id)) return new(message: "User is not authenticated.", statusCode: 401, code: "UNAUTHENTICATED");
            var user = await _users.FindByIdAsync(id);
            if (user is null) return new(message: "User not found.", statusCode: 404, code: "USER_NOT_FOUND");
            if (!user.IsActive) return new(message: "User is inactive.", statusCode: 403, code: "USER_INACTIVE");
            return new(user);
        }
        private IActionResult ValidationError() => Result(new ResponseStatus<bool>(message: "Validation failed.", errors: ModelState.Values.SelectMany(v => v.Errors).Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToList(), statusCode: 400, code: "VALIDATION_ERROR"));
        private IActionResult Result<T>(ResponseStatus<T> response) => StatusCode(response.StatusCode, response);
    }

}
