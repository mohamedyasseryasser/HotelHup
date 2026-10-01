using System.Security.Claims;
using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.Expense;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Reports;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelHup.API.Controllers;

[ApiController]
[Authorize(Policy = Permissions.Reports.Read)]
[Route("api/v1/reports")]
public sealed class ReportController : ControllerBase
{
    private readonly IReportService _service;
    private readonly UserManager<User> _users;

    public ReportController(
        IReportService service,
        UserManager<User> users)
    {
        _service = service;
        _users = users;
    }

    [HttpGet("occupancy")]
    public Task<IActionResult> Occupancy(
        [FromQuery] OccupancyReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetOccupancyAsync(request, actor, ct));
    }

    [HttpGet("revenue")]
    public Task<IActionResult> Revenue(
        [FromQuery] RevenueReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetRevenueAsync(request, actor, ct));
    }

    [HttpGet("arrivals")]
    public Task<IActionResult> Arrivals(
        [FromQuery] ReservationReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetArrivalsAsync(request, actor, ct));
    }

    [HttpGet("departures")]
    public Task<IActionResult> Departures(
        [FromQuery] ReservationReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetDeparturesAsync(request, actor, ct));
    }

    [HttpGet("current-guests")]
    public Task<IActionResult> CurrentGuests(
        [FromQuery] ReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetCurrentGuestsAsync(request, actor, ct));
    }

    [HttpGet("room-status")]
    public Task<IActionResult> RoomStatus(
        [FromQuery] RoomStatusReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetRoomStatusAsync(request, actor, ct));
    }

    [HttpGet("payments")]
    public Task<IActionResult> Payments(
        [FromQuery] PaymentReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetPaymentsAsync(request, actor, ct));
    }

    [HttpGet("refunds")]
    public Task<IActionResult> Refunds(
        [FromQuery] RefundReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetRefundsAsync(request, actor, ct));
    }

    [HttpGet("cancellations")]
    public Task<IActionResult> Cancellations(
        [FromQuery] CancellationReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetCancellationsAsync(request, actor, ct));
    }

    [HttpGet("housekeeping")]
    public Task<IActionResult> Housekeeping(
        [FromQuery] HousekeepingReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetHousekeepingAsync(request, actor, ct));
    }

   

    [HttpGet("expenses")]
    public Task<IActionResult> Expenses(
        [FromQuery] ExpenseReportRequest request,
        CancellationToken ct)
    {
        return Execute(
            request,
            actor => _service.GetExpenseReportAsync(request, actor, ct));
    }

    private async Task<IActionResult> Execute<TRequest, TResult>(
        TRequest request,
        Func<User, Task<ResponseStatus<TResult>>> operation)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            return StatusCode(
                400,
                new ResponseStatus<bool>(
                    "Validation failed.",
                    errors,
                    400,
                    "VALIDATION_ERROR"));
        }

        var actorResult = await GetActorAsync();

        if (!actorResult.Success || actorResult.Data is null)
        {
            return StatusCode(
                actorResult.StatusCode,
                actorResult);
        }

        var result = await operation(actorResult.Data);

        return StatusCode(
            result.StatusCode,
            result);
    }

    private async Task<ResponseStatus<User>> GetActorAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id))
        {
            return new ResponseStatus<User>(
                "User is not authenticated.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        var actor = await _users.FindByIdAsync(id);

        if (actor is null)
        {
            return new ResponseStatus<User>(
                "User not found.",
                statusCode: 404,
                code: "USER_NOT_FOUND");
        }

        if (!actor.IsActive)
        {
            return new ResponseStatus<User>(
                "User is inactive.",
                statusCode: 403,
                code: "USER_INACTIVE");
        }

        return new ResponseStatus<User>(actor);
    }
}