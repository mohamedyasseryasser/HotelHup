using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
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
    [Route("api/v1/propertiecancellationpolicy")]
    public class CancellationPoicyController : Controller
    {
        private readonly ICancellationPolicyService _service;
        private readonly UserManager<User> usermanager;

        public CancellationPoicyController(ICancellationPolicyService service, UserManager<User> usermanager)
        {
            _service = service;
            this.usermanager = usermanager;
        }
        private async Task<ResponseStatus<User>> GetCurrentuserLogeined()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userid == null)
            {
                return new ResponseStatus<User>(statusCode: 401, message: "user is not authenticated");
            }
            var user = await usermanager.FindByIdAsync(userid);
            if (user == null)
            {
                return new ResponseStatus<User>(
                    "User not found",
                    statusCode: 404
                );
            }
            return new ResponseStatus<User>(user);
        }
        private IActionResult? ValidateModelState()
        {
            if (ModelState.IsValid)
                return null;

            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Invalid value."
                        : e.ErrorMessage)
                .ToList();

            var response = new ResponseStatus<bool>(
                message: "Validation failed.",
                errors: errors,
                statusCode: 400);

            return Result(response);
        }


        private IActionResult Result<T>(ResponseStatus<T> response)
        {
            return StatusCode(response.StatusCode, response);
        }
        [HttpGet("{propertyId:int}/cancellation-policies")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> CancellationPolicies(
            int propertyId,
            CancellationToken ct)
        {
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result =
                await _service.GetCancellationPoliciesAsync(
                    propertyId,
                    ct);

            return Result(result);
        }

        [HttpGet("{propertyId:int}/cancellation-policies/{policyId:int}")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> CancellationPolicy(
            int propertyId,
            int policyId,
            CancellationToken ct)
        {//authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result =
                await _service.GetCancellationPolicyAsync(
                   CurrentUserLogin.Data, propertyId,
                    policyId,
                    ct);

            return Result(result);
        }

        [HttpPost("{propertyId:int}/cancellation-policies")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> CreateCancellationPolicy(
            int propertyId,
            CreateCancellationPolicyRequest request,
            CancellationToken ct)
        {
            //modelstate validation
            var validationResult = ValidateModelState();
            if (validationResult is not null)
                return validationResult;
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.CreateCancellationPolicyAsync(CurrentUserLogin.Data, propertyId, request, ct);
            return Result(result);
        }

        [HttpPatch("{propertyId:int}/cancellation-policies/{policyId:int}")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> UpdateCancellationPolicy(
            int propertyId,
            int policyId,
            UpdateCancellationPolicyRequest request, string ifmatch,
            CancellationToken ct)
        {
            //modelstate validation
            var validationResult = ValidateModelState();
            if (validationResult is not null)
                return validationResult;
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result =
                await _service.UpdateCancellationPolicyAsync(
                   CurrentUserLogin.Data, propertyId,
                    policyId,
                    request,
                    ct);

            return Result(result);
        }

        [HttpPost(
            "{propertyId:int}/cancellation-policies/{policyId:int}/activate")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> ActivateCancellationPolicy(
            int propertyId,
            int policyId, ChangeCancellationPolicyStatusRequest r,
            CancellationToken ct)
        {

            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result =
                await _service.SetCancellationPolicyStatusAsync(
                   CurrentUserLogin.Data, propertyId,
                    policyId,
                   true, r,
                    ct);

            return Result(result);
        }

        [HttpPost(
            "{propertyId:int}/cancellation-policies/{policyId:int}/deactivate")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> DeactivateCancellationPolicy(
            int propertyId,
            int policyId, ChangeCancellationPolicyStatusRequest r,
            CancellationToken ct)
        {

            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result =
                await _service.SetCancellationPolicyStatusAsync(
                   CurrentUserLogin.Data, propertyId,
                    policyId,
                    false, r,
                    ct);

            return Result(result);
        }
    }
}
