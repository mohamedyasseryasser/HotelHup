using HotelHup.APPLICATION.DTO.Auth;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.user;
using HotelHup.APPLICATION.services.implementation;
using HotelHup.APPLICATION.services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace HotelHup.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                  .Values
                    .SelectMany(x => x.Errors)
                      .Select(x => x.ErrorMessage)
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                         .ToList();

                var results = new ResponseStatus<UserListRequestDto>(
                    message: "Invalid request",
                    errors: errors,
                    statusCode: 400
                );
                return ToActionResult(results);
            }
            var result = await _authService.LoginAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshAsync(request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _authService.LogoutAsync(userId ?? string.Empty, cancellationToken);
            return ToActionResult(result);
        }

        private IActionResult ToActionResult<T>(HotelHup.APPLICATION.DTO.General.ResponseStatus<T> result)
        {
            return result.StatusCode switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                401 => Unauthorized(result),
                403 => Forbid(),
                _ => StatusCode(result.StatusCode, result)
            };
        }
    }
}
