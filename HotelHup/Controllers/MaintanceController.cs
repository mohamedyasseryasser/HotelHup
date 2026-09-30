using System.Security.Claims;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.maintenance;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelHup.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/maintenance")]
public sealed class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _service;
    private readonly UserManager<User> _users;

    public MaintenanceController(
        IMaintenanceService service,
        UserManager<User> users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Maintenance.Read)]
    public async Task<IActionResult> List(
        [FromQuery] MaintenanceListRequest request,
        CancellationToken ct)
    {
        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.GetListAsync(actor.Data, request, ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Maintenance.Read)]
    public async Task<IActionResult> Get(
        int id,
        [FromQuery] int propertyId,
        CancellationToken ct)
    {
        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.GetAsync(actor.Data, propertyId, id, ct));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Maintenance.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceRequest request,
        CancellationToken ct)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.CreateAsync(actor.Data, request, ct));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Maintenance.Update)]
    public async Task<IActionResult> Update(
        int id,
        [FromQuery] int propertyId,
        [FromBody] UpdateMaintenanceRequest request,
        CancellationToken ct)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.UpdateAsync(
            actor.Data,
            propertyId,
            id,
            request,
            ct));
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Policy = Permissions.Maintenance.Assign)]
    public async Task<IActionResult> Assign(
        int id,
        [FromQuery] int propertyId,
        [FromBody] AssignMaintenanceRequest request,
        CancellationToken ct)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.AssignAsync(
            actor.Data,
            propertyId,
            id,
            request,
            ct));
    }

    [HttpPost("{id:int}/start")]
    [Authorize(Policy = Permissions.Maintenance.Update)]
    public async Task<IActionResult> Start(
        int id,
        [FromQuery] int propertyId,
        [FromBody] MaintenanceActionRequest? request,
        CancellationToken ct)
    {
        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.StartAsync(
            actor.Data,
            propertyId,
            id,
            request ?? new MaintenanceActionRequest(),
            ct));
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Policy = Permissions.Maintenance.Complete)]
    public async Task<IActionResult> Complete(
        int id,
        [FromQuery] int propertyId,
        [FromBody] MaintenanceActionRequest? request,
        CancellationToken ct)
    {
        var actor = await GetActorAsync(ct);
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.CompleteAsync(
            actor.Data,
            propertyId,
            id,
            request ?? new MaintenanceActionRequest(),
            ct));
    }

    private async Task<ResponseStatus<User>> GetActorAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ResponseStatus<User>(
                "User is not authenticated.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            return new ResponseStatus<User>(
                "User was not found.",
                statusCode: 404,
                code: "USER_NOT_FOUND");
        }

        if (!user.IsActive)
        {
            return new ResponseStatus<User>(
                "User is inactive.",
                statusCode: 403,
                code: "USER_INACTIVE");
        }

        return new ResponseStatus<User>(user);
    }

    private IActionResult? ValidateModelState()
    {
        if (ModelState.IsValid)
        {
            return null;
        }

        var errors = ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "Invalid value."
                : error.ErrorMessage)
            .ToList();

        return Result(new ResponseStatus<bool>(
            "Validation failed.",
            errors,
            400,
            "VALIDATION_ERROR"));
    }

    private IActionResult Result<T>(ResponseStatus<T> response)
    {
        return StatusCode(response.StatusCode, response);
    }
}
