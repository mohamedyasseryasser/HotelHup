using HotelHup.APPLICATION.DTO.auth;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelHup.API.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<ResponseStatus<TokenResponseDto>>> Login([FromBody] LoginDTO? request)
        {
            if (request is null || !ModelState.IsValid)
                return ValidationError<TokenResponseDto>();

            var response = await _authService.LoginAsync(request);
            return ToActionResult(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ResponseStatus<TokenResponseDto>>> RefreshToken([FromBody] RefreshTokenDto? request)
        {
            if (request is null || !ModelState.IsValid)
                return ValidationError<TokenResponseDto>();

            var response = await _authService.RefreshTokenAsync(request);
            return ToActionResult(response);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ResponseStatus<bool>>> Logout([FromBody] RefreshTokenDto? request)
        {
            if (request is null || !ModelState.IsValid)
                return ValidationError<bool>();

            var response = await _authService.LogoutAsync(request.RefreshToken);
            return ToActionResult(response);
        }

 

        [Authorize]
        [HttpPut("change-role")]
        public async Task<ActionResult<ResponseStatus<bool>>> ChangeRole([FromBody] ChangeRoleDTO? request)
        {
            if (request is null || !ModelState.IsValid)
                return ValidationError<bool>();

            var response = await _authService.ChangeRoleAsync(request);
            return ToActionResult(response);
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<ActionResult<ResponseStatus<bool>>> ChangePassword([FromBody] ChangePasswordDTO? request)
        {
            if (request is null || !ModelState.IsValid)
                return ValidationError<bool>();

            var response = await _authService.ChangePasswordAsync(request);
            return ToActionResult(response);
        }

        private ActionResult<ResponseStatus<T>> ValidationError<T>()
        {
            var errors = ModelState.Values
                .SelectMany(state => state.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "The request contains an invalid value."
                    : error.ErrorMessage)
                .Distinct()
                .ToList();

            if (errors.Count == 0)
                errors.Add("The request is invalid.");

            return BadRequest(new ResponseStatus<T>(
                default!,
                "Request validation failed.",
                false,
                errors));
        }

        private ActionResult<ResponseStatus<T>> ToActionResult<T>(ResponseStatus<T> response)
        {
            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }
    }
}

