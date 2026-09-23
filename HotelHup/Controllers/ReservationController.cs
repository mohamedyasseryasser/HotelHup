using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelHup.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ReservationController : ControllerBase
{
    private readonly UserManager<User> _users;
    private readonly IReservationService _service;

    public ReservationController(UserManager<User> users, IReservationService service)
    {
        _users = users;
        _service = service;
    }

    [HttpGet("availability")]
    [Authorize(Policy = Permissions.Reservations.Read)]
    public async Task<IActionResult> Availability([FromQuery] AvailabilityRequest request, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.SearchAvailabilityAsync(actor.Data, request, ct));
    }

    [HttpGet("reservations")]
    [Authorize(Policy = Permissions.Reservations.Read)]
    public async Task<IActionResult> List([FromQuery] ReservationSearchRequest request, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.ListAsync(actor.Data, request, ct));
    }

    [HttpGet("reservations/{id:int}")]
    [Authorize(Policy = Permissions.Reservations.Read)]
    public async Task<IActionResult> Get(int id, [FromQuery] int propertyId, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.GetAsync(actor.Data, propertyId, id, ct));
    }

    [HttpGet("reservations/{id:int}/status-history")]
    [Authorize(Policy = Permissions.Reservations.Read)]
    public async Task<IActionResult> StatusHistory(int id, [FromQuery] int propertyId, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.GetStatusHistoryAsync(actor.Data, propertyId, id, ct));
    }

    [HttpPost("reservations")]
    [Authorize(Policy = Permissions.Reservations.Create)]
    public async Task<IActionResult> Create
        (
        [FromBody] CreateReservationRequest request,
        CancellationToken ct)
    {
        var invalid = ValidateModelState();
        {
            if (invalid is not null) return invalid;
        }
        var actor = await GetActor();
        {
            if (!actor.Success || actor.Data is null)
                return Result(actor);
        }
        return Result(await _service.CreateAsync(actor.Data, request, ct));
    }

    [HttpPut("reservations/{id:int}")]
    [Authorize(Policy = Permissions.Reservations.Update)]
    public async Task<IActionResult> Update
        (int id,
        [FromQuery] int propertyId, 
        [FromBody] UpdateReservationRequest request,
        CancellationToken ct)
    {
        var invalid = ValidateModelState();
        {
            if (invalid is not null) return invalid;
        }
        var actor = await GetActor();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }
        return Result(await _service.UpdateAsync(actor.Data, propertyId, id, request, ct));
    }

    [HttpPost("reservations/{id:int}/confirm")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> 
        Confirm(int id,
        [FromQuery] int propertyId, 
        [FromBody] ConfirmReservationRequest? request,
        CancellationToken ct)
    {
        var actor = await GetActor();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }
        return Result(await _service.ConfirmAsync(actor.Data, propertyId, id, request ?? new(), ct));
    }

    [HttpPost("reservations/{id:int}/cancel")]
    [Authorize(Policy = Permissions.Reservations.Cancel)]
    public async Task<IActionResult> Cancel(int id, [FromQuery] int propertyId, [FromBody] CancelReservationRequest request, CancellationToken ct)
    {
        var invalid = ValidateModelState(); if (invalid is not null) return invalid;
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.CancelAsync(actor.Data, propertyId, id, request, ct));
    }

    [HttpPost("reservations/{id:int}/no-show")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> NoShow(int id, [FromQuery] int propertyId, [FromBody] NoShowRequest? request, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.NoShowAsync(actor.Data, propertyId, id, request ?? new(), ct));
    }

    [HttpPost("reservations/{id:int}/rooms")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> AssignRoom(int id, [FromQuery] int propertyId, [FromBody] AssignRoomRequest request, CancellationToken ct)
    {
        var invalid = ValidateModelState(); if (invalid is not null) return invalid;
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.AssignRoomAsync(actor.Data, propertyId, id, request, ct));
    }

    [HttpDelete("reservations/{id:int}/rooms/{reservationRoomId:int}")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> ReleaseRoom(int id, int reservationRoomId, [FromQuery] int propertyId, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.ReleaseRoomAsync(actor.Data, propertyId, id, reservationRoomId, ct));
    }

    [HttpPost("reservations/{id:int}/check-in")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> CheckIn(int id, [FromQuery] int propertyId, [FromBody] CheckInRequest request, CancellationToken ct)
    {
        var invalid = ValidateModelState(); if (invalid is not null) return invalid;
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.CheckInAsync(actor.Data, propertyId, id, request, ct));
    }

    [HttpPost("reservations/{id:int}/check-out")]
    [Authorize(Policy = Permissions.Reservations.Modify)]
    public async Task<IActionResult> CheckOut(int id, [FromQuery] int propertyId, [FromBody] CheckOutRequest? request, CancellationToken ct)
    {
        var actor = await GetActor(); if (!actor.Success || actor.Data is null) return Result(actor);
        return Result(await _service.CheckOutAsync(actor.Data, propertyId, id, request ?? new(), ct));
    }

    private async Task<ResponseStatus<User>> GetActor()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(id)) return new ResponseStatus<User>(message: "User is not authenticated.", statusCode: 401, code: "UNAUTHENTICATED");
        var user = await _users.FindByIdAsync(id);
        if (user is null) return new ResponseStatus<User>(message: "User not found.", statusCode: 404, code: "USER_NOT_FOUND");
        if (!user.IsActive) return new ResponseStatus<User>(message: "User is inactive.", statusCode: 403, code: "USER_INACTIVE");
        return new ResponseStatus<User>(user);
    }

    private IActionResult? ValidateModelState()
    {
        if (ModelState.IsValid) return null;
        var errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToList();
        return Result(new ResponseStatus<bool>(message: "Validation failed.", errors: errors, statusCode: 400, code: "VALIDATION_ERROR"));
    }

    private IActionResult Result<T>(ResponseStatus<T> response) => StatusCode(response.StatusCode, response);
}
