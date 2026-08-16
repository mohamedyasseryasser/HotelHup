using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelHup.API.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("add")]
        public async Task<ActionResult<ResponseStatus<responseuserdto>>> Add(
            [FromBody] AddUserDto? request,
            [FromQuery] UserRole role )
        {
            if (request is null || !ModelState.IsValid)
            {
                return ValidationError<responseuserdto>();
            }

            var response = await _userService.AddAsync(request, role);
            return ToActionResult(response);
        }

        [HttpPut("update")]
        public async Task<ActionResult<ResponseStatus<responseuserdto>>> Update(
            [FromBody] UpdateUserDto? request,
            [FromQuery] UserRole role = UserRole.Receptionist)
        {
            if (request is null || !ModelState.IsValid)
            {
                return ValidationError<responseuserdto>();
            }

            var response = await _userService.UpdateAsync(request, role);
            return ToActionResult(response);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<ResponseStatus<responseuserdto>>> GetById(
            [FromRoute] string? userId,
            [FromQuery] UserRole role = UserRole.Receptionist)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError(nameof(userId), "User id is required.");
                return ValidationError<responseuserdto>();
            }

            var response = await _userService.GetByIdAsync(userId, role);
            return ToActionResult(response);
        }

        [HttpGet]
        public async Task<ActionResult<ResponseStatus<IEnumerable<responseuserdto>>>> GetAll(
            [FromQuery] pagination? request,
            [FromQuery] UserRole role = UserRole.Receptionist,
            [FromQuery] string? fullname = null,
            [FromQuery] string? email = null)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError<IEnumerable<responseuserdto>>();
            }

            var response = await _userService.GetAllAsync(
                request ?? new pagination(), role, fullname, email);
            return ToActionResult(response);
        }

        [HttpGet("active")]
        public async Task<ActionResult<ResponseStatus<IEnumerable<responseuserdto>>>> GetAllActive(
            [FromQuery] UserRole role = UserRole.Receptionist)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError<IEnumerable<responseuserdto>>();
            }

            var response = await _userService.GetAllActiveAsync(role);
            return ToActionResult(response);
        }

        [HttpGet("count")]
        public async Task<ActionResult<ResponseStatus<int>>> GetCount(
            [FromQuery] UserRole role = UserRole.Receptionist)
        {
            if (!ModelState.IsValid)
            {
                return ValidationError<int>();
            }

            var response = await _userService.GetCountAsync(role);
            return ToActionResult(response);
        }

        [HttpDelete("{userId}")]
        public async Task<ActionResult<ResponseStatus<responseuserdto>>> Remove(
            [FromRoute] string? userId,
            [FromQuery] UserRole role = UserRole.Receptionist)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError(nameof(userId), "User id is required.");
                return ValidationError<responseuserdto>();
            }

            var response = await _userService.RemoveAsync(userId, role);
            return ToActionResult(response);
        }

        private ActionResult<ResponseStatus<T>> ValidationError<T>()
        {
            var errors = ModelState.Values
                .SelectMany(state => state.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "The request contains an invalid value."
                    : error.ErrorMessage)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (errors.Count == 0)
            {
                errors.Add("The request is invalid.");
            }

            return BadRequest(new ResponseStatus<T>(
                default!,
                "Request validation failed.",
                false,
                errors));
        }

        private ActionResult<ResponseStatus<T>> ToActionResult<T>(ResponseStatus<T> response)
        {
            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }
    }
}