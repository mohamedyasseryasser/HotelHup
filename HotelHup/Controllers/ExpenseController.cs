using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.Expense;
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
[Route("api/v1/expenses")]
public sealed class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _service;
        private readonly UserManager<User> _users;

        public ExpenseController(
            IExpenseService service,
            UserManager<User> users)
        {
            _service = service;
            _users = users;
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Expenses.Create)]
        public async Task<IActionResult> Create(
            CreateExpenseDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Validation();
            }

            var a = await Actor();

            return !a.Success || a.Data is null
                ? Result(a)
                : Result(await _service.CreateAsync(dto, a.Data, ct));
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Expenses.Read)]
        public async Task<IActionResult> List(
            [FromQuery] ExpenseQueryDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Validation();
            }

            var a = await Actor();

            return !a.Success || a.Data is null
                ? Result(a)
                : Result(await _service.GetPagedAsync(dto, a.Data, ct));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Expenses.Read)]
        public async Task<IActionResult> Get(
            int id,
            CancellationToken ct)
        {
            var a = await Actor();

            return !a.Success || a.Data is null
                ? Result(a)
                : Result(await _service.GetByIdAsync(id, a.Data, ct));
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = Permissions.Expenses.Update)]
        public async Task<IActionResult> Update(
            int id,
            UpdateExpenseDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Validation();
            }

            var a = await Actor();

            return !a.Success || a.Data is null
                ? Result(a)
                : Result(await _service.UpdateAsync(id, dto, a.Data, ct));
        }

        [HttpPost("{id:int}/void")]
        [Authorize(Policy = Permissions.Expenses.Void)]
        public async Task<IActionResult> Void(
            int id,
            VoidExpenseDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return Validation();
            }

            var a = await Actor();

            return !a.Success || a.Data is null
                ? Result(a)
                : Result(await _service.VoidAsync(id, dto, a.Data, ct));
        }

        private async Task<ResponseStatus<User>> Actor()
        {
            var id =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(id))
            {
                return new ResponseStatus<User>(
                    "User is not authenticated.",
                    statusCode: 401);
            }

            var u = await _users.FindByIdAsync(id);

            if (u is null)
            {
                return new ResponseStatus<User>(
                    "User not found.",
                    statusCode: 404);
            }

            if (!u.IsActive)
            {
                return new ResponseStatus<User>(
                    "User is inactive.",
                    statusCode: 403);
            }

            return new ResponseStatus<User>(u);
        }

        private IActionResult Validation()
        {
            return Result(
                new ResponseStatus<bool>(
                    message: "Validation failed.",
                    errors: ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList(),
                    statusCode: 400,
                    code: "VALIDATION_ERROR"));
        }

        private IActionResult Result<T>(ResponseStatus<T> r)
        {
            return StatusCode(r.StatusCode, r);
        }
    }
}
