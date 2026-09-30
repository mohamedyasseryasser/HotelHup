using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.services;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelHup.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/services")]
public sealed class ServicesController : ControllerBase
{
    private readonly IServiceService _service;
    private readonly UserManager<User> _users;

    public ServicesController(
        IServiceService service,
        UserManager<User> users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Services.Read)]
    public async Task<IActionResult> List(
        [FromQuery] ServiceListRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetCurrentUserAsync();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.GetListAsync(
            actor.Data,
            request,
            cancellationToken));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Services.Read)]
    public async Task<IActionResult> Get(
        int id,
        [FromQuery] int propertyId,
        CancellationToken cancellationToken)
    {
        var actor = await GetCurrentUserAsync();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.GetAsync(
            actor.Data,
            propertyId,
            id,
            cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Services.Create)]
    public async Task<IActionResult> Create(
        [FromQuery] int propertyId,
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetCurrentUserAsync();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.CreateAsync(
            actor.Data,
            propertyId,
            request,
            cancellationToken));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Policy = Permissions.Services.Update)]
    public async Task<IActionResult> Update(
        int id,
        [FromQuery] int propertyId,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetCurrentUserAsync();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.UpdateAsync(
            actor.Data,
            propertyId,
            id,
            request,
            cancellationToken));
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = Permissions.Services.Deactivate)]
    public async Task<IActionResult> Deactivate(
        int id,
        [FromQuery] int propertyId,
        [FromBody] DeactivateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateModelState();
        if (validation is not null)
        {
            return validation;
        }

        var actor = await GetCurrentUserAsync();
        if (!actor.Success || actor.Data is null)
        {
            return Result(actor);
        }

        return Result(await _service.DeactivateAsync(
            actor.Data,
            propertyId,
            id,
            request,
            cancellationToken));
    }

    private async Task<ResponseStatus<User>> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ResponseStatus<User>(
                message: "User is not authenticated.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            return new ResponseStatus<User>(
                message: "User not found.",
                statusCode: 404,
                code: "USER_NOT_FOUND");
        }

        if (!user.IsActive)
        {
            return new ResponseStatus<User>(
                message: "User is inactive.",
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
            message: "Validation failed.",
            errors: errors,
            statusCode: 400,
            code: "VALIDATION_ERROR"));
    }

    private IActionResult Result<T>(ResponseStatus<T> response)
    {
        return StatusCode(response.StatusCode, response);
    }
}
