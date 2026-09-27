using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Payments;
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
    [Route("api/v1/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _service;
        private readonly UserManager<User> _users;
        public PaymentsController(IPaymentService service, UserManager<User> users) { _service = service; _users = users; }

        [HttpPost("folios/{folioId:int}")]
        [Authorize(Policy = Permissions.Payments.Create)]
        public async Task<IActionResult>
            Create(
            int folioId, 
            [FromBody] CreatePaymentRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError();
            }
            var actor = await Actor();
            {
                if (!actor.Success || actor.Data is null) return Result(actor);
            }
            Request.Headers.TryGetValue("Idempotency-Key", out var header);
            return Result(  await _service.CreatePaymentAsync(actor.Data, folioId, request, header.FirstOrDefault(), ct));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Payments.Read)]
        public async Task<IActionResult>
            Get(
            int id,
            CancellationToken ct)
        {
            var actor = await Actor();
            if (!actor.Success || actor.Data is null)
            {
                return Result(actor);
            }
            return Result(await _service.GetPaymentByIdAsync(actor.Data, id, ct));
        }

        [HttpGet("folios/{folioId:int}")]
        [Authorize(Policy = Permissions.Payments.Read)]
        public async Task<IActionResult>
            GetByFolio(
            int folioId, 
            CancellationToken ct)
        {
            var actor = await Actor();
            if (!actor.Success || actor.Data is null)
            {
                return Result(actor);
            }
            return Result(await _service.GetPaymentsByFolioAsync(actor.Data, folioId, ct));
        }

        [HttpGet("folios/{folioId:int}/summary")]
        [Authorize(Policy = Permissions.Payments.Read)]
        public async Task<IActionResult> 
            Summary(
            int folioId,
            CancellationToken ct)
        {
            var actor = await Actor();
            if (!actor.Success || actor.Data is null)
            {
                return Result(actor);
            }
            return Result(await _service.GetSummaryAsync(actor.Data, folioId, ct));
        }

        [HttpPost("{id:int}/refunds")]
        [Authorize(Policy = Permissions.Refunds.Create)]
        public async Task<IActionResult> 
            Refund(
            int id,
            [FromBody] RefundPaymentRequest request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError();
            }
            var actor = await Actor();
            {
                if (!actor.Success || actor.Data is null) return Result(actor);
            }
            return Result(await _service.RefundPaymentAsync(actor.Data, id, request, ct));
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
