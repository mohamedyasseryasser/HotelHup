using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property.propertytaxdto;
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
    [Route("api/v1/propertietax")]
    public class PropertyTaxController : Controller
    {
        public UserManager<User> Usermanager { get; }
        public IPropertyTax _service { get; }

        public PropertyTaxController(UserManager<User> usermanager,IPropertyTax propertyTax)
        {
            Usermanager = usermanager;
            _service = propertyTax;
        }

        private async Task<ResponseStatus<User>> GetCurrentuserLogeined()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userid == null)
            {
                return new ResponseStatus<User>(statusCode: 401, message: "user is not authenticated");
            }
            var user = await Usermanager.FindByIdAsync(userid);
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

        [HttpGet("{propertyId:int}/taxes")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> Taxes(
                  int propertyId,
                  CancellationToken ct)
        {
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.GetTaxesAsync(CurrentUserLogin.Data, propertyId, ct);
            return Result(result);
        }

        [HttpGet("{propertyId:int}/taxes/{taxId:int}")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> Tax(
            int propertyId,
            int taxId,
            CancellationToken ct)
        {  //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.GetTaxAsync(
                CurrentUserLogin.Data, propertyId,
                taxId,
                ct);

            return Result(result);
        }

        [HttpPost("{propertyId:int}/taxes")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> CreateTax(
            int propertyId,
            CreateTaxRequest request,
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
            var result = await _service.CreateTaxAsync(
               CurrentUserLogin.Data, propertyId,
                request,
                ct);
            return Result(result);
        }

        [HttpPatch("{propertyId:int}/taxes/{taxId:int}")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> UpdateTax(
            int propertyId,
            int taxId,
            UpdateTaxRequest request,string ifmatch,
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
            var result = await _service.UpdateTaxAsync(
               CurrentUserLogin.Data, propertyId,
                taxId,
                request,ifmatch,
                ct);

            return Result(result);
        }

        [HttpPost(
       "{propertyId:int}/taxes/{taxId:int}/activate")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> ActivateTax(
       int propertyId,
       int taxId,
       [FromHeader(Name = "If-Match")]
    string? ifMatch,
       CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return StatusCode(
                    StatusCodes.Status428PreconditionRequired,
                    new
                    {
                        message =
                            "If-Match header is required."
                    });
            }

            var currentUser =
                await GetCurrentuserLogeined();

            if (!currentUser.Success ||
                currentUser.Data is null)
            {
                return Result(currentUser);
            }

            var result =
                await _service.ActivateTaxAsync(
                    currentUser.Data,
                    propertyId,
                    taxId,
                    ifMatch,
                    ct);

            return Result(result);
        }


        [HttpPost(
            "{propertyId:int}/taxes/{taxId:int}/deactivate")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> DeactivateTax(
            int propertyId,
            int taxId,
            [FromHeader(Name = "If-Match")]
    string? ifMatch,
            [FromBody] ChangeTaxStatusRequest request,
            CancellationToken ct)
        {
            var validationResult =
                ValidateModelState();

            if (validationResult is not null)
            {
                return validationResult;
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return StatusCode(
                    StatusCodes.Status428PreconditionRequired,
                    new
                    {
                        message =
                            "If-Match header is required."
                    });
            }

            var currentUser =
                await GetCurrentuserLogeined();

            if (!currentUser.Success ||
                currentUser.Data is null)
            {
                return Result(currentUser);
            }

            var result =
                await _service.DeactivateTaxAsync(
                    currentUser.Data,
                    propertyId,
                    taxId,
                    ifMatch,
                    request,
                    ct);

            return Result(result);
        }

    }
}
