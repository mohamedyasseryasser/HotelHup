using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
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
    [Route("api/v1/propertiesdeposit")]
    public class DepositPolicyController : Controller
    {
        private readonly IPropertyDepositService _service;
        private readonly UserManager<User> usermanager;

        public DepositPolicyController(IPropertyDepositService service, UserManager<User> usermanager)
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
        [HttpGet("{propertyId:int}/deposit-policies")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> DepositPolicies(
        int propertyId,
        CancellationToken ct)
        {
            var user = await GetCurrentuserLogeined();

            if (!user.Success || user.Data == null)
                return Result(user);

            var result =
                await _service.GetDepositPoliciesAsync(
                    user.Data,
                    propertyId,
                    ct);

            return Result(result);
        }

        [HttpGet("{propertyId:int}/deposit-policies/{policyId:int}")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> DepositPolicy(
            int propertyId,
            int policyId,
            CancellationToken ct)
        {
            var user = await GetCurrentuserLogeined();

            if (!user.Success || user.Data == null)
                return Result(user);

            var result =
                await _service.GetDepositPolicyAsync(
                    user.Data,
                    propertyId,
                    policyId,
                    ct);

            return Result(result);
        }

        [HttpPost("{propertyId:int}/deposit-policies")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> CreateDepositPolicy(
            int propertyId,
            CreateDepositPolicyRequest request,
            CancellationToken ct)
        {
            var validationResult =
         ValidateModelState();

            if (validationResult is not null)
                return validationResult;

            var user =
                await GetCurrentuserLogeined();

            if (!user.Success || user.Data == null)
                return Result(user);

            var result =
                await _service.CreateDepositPolicyAsync(
                    user.Data,
                    propertyId,
                    request,
                    ct);

            return Result(result);
        }

        [HttpPatch("{propertyId:int}/deposit-policies/{policyId:int}")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> UpdateDepositPolicy(
            int propertyId,
            int policyId,
            UpdateDepositPolicyRequest request, string ifmatch,
            CancellationToken ct)
        {
            var validationResult =
         ValidateModelState();

            if (validationResult is not null)
                return validationResult;

            var user =
                await GetCurrentuserLogeined();

            if (!user.Success || user.Data == null)
                return Result(user);
            var result =
                await _service.UpdateDepositPolicyAsync(
               user.Data, propertyId,
                    policyId,
                    request, ifmatch,
                    ct);

            return Result(result);
        }

        [HttpPost(
        "{propertyId:int}/deposit-policies/{policyId:int}/activate")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> ActivateDepositPolicy(
        int propertyId,
        int policyId,
        [FromBody] ChangeDepositPolicyStatusRequest? request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
        {
            var user =
                await GetCurrentuserLogeined();

            if (!user.Success || user.Data == null)
                return Result(user);

            var result =
                await _service.SetDepositPolicyStatusAsync(
                    user.Data,
                    propertyId,
                    policyId,
                    true,
                    request?.Reason,
                    request?.IfMatch ?? ifMatch,
                    ct);

            return Result(result);
        }


        [HttpPost(
    "{propertyId:int}/deposit-policies/{policyId:int}/deactivate")]
[Authorize(Policy = Permissions.Properties.Update)]
public async Task<IActionResult> DeactivateDepositPolicy(
    int propertyId,
    int policyId,
    [FromBody] ChangeDepositPolicyStatusRequest? request,
    [FromHeader(Name = "If-Match")] string? ifMatch,
    CancellationToken ct)
{
    var user =
        await GetCurrentuserLogeined();

    if (!user.Success || user.Data == null)
        return Result(user);

    var result =
        await _service.SetDepositPolicyStatusAsync(
            user.Data,
            propertyId,
            policyId,
            false,
            request?.Reason,
            request?.IfMatch ?? ifMatch,
            ct);

    return Result(result);
}

    }
}
