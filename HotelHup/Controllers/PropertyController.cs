using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.property;
using HotelHup.APPLICATION.DTO.property.propertycancellationdtos;
using HotelHup.APPLICATION.DTO.property.propertydepositdtos;
using HotelHup.APPLICATION.DTO.property.propertydto;
using HotelHup.APPLICATION.DTO.property.propertysettingdto;
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
    [Route("api/v1/properties")]
    public sealed class PropertyController : ControllerBase
    {
        private readonly IPropertyService _service;
        private readonly UserManager<User> usermanager;

        public PropertyController(IPropertyService service,UserManager<User> usermanager)
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

        [HttpGet]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> List(
            [FromQuery] PropertyListRequest request,
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
            var result = await _service.GetListAsync(CurrentUserLogin.Data,request, ct);

            return Result(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> Get(
            int id,
            CancellationToken ct)
        {   
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.GetAsync(CurrentUserLogin.Data,id, ct);

            return Result(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Properties.Create)]
        public async Task<IActionResult> Create(
            CreatePropertyRequest request,
            CancellationToken ct)
        {
            //modelstate validation
            var validationResult = ValidateModelState();
            if (validationResult is not null)
                return validationResult;
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success||CurrentUserLogin.Data==null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.CreateAsync(CurrentUserLogin.Data,request, ct);

            return Result(result);
        }

        [HttpPatch("{id:int}")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> Update(
            int id,string ifmatch,
            UpdatePropertyRequest request,                            
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
            var result = await _service.UpdateAsync(CurrentUserLogin.Data,ifmatch,id, request, ct);

            return Result(result);
        }

        [HttpPost("{id:int}/activate")]
        [Authorize(Policy = Permissions.Properties.Activate)]
        public async Task<IActionResult> Activate(
            [FromHeader(Name = "If-Match")] string expected,
            int id,
            CancellationToken ct)
        {
            var CurrentUserLogin =
                await GetCurrentuserLogeined();

            if (!CurrentUserLogin.Success ||
                CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }

            var result =
                await _service.ActivateAsync(
                    expected,
                    CurrentUserLogin.Data,
                    id,
                    ct);

            return Result(result);
        }


        [HttpPost("{id:int}/deactivate")]
        [Authorize(Policy = Permissions.Properties.Deactivate)]
        public async Task<IActionResult> Deactivate(
       [FromHeader(Name = "If-Match")] string expected,
       int id,
       [FromBody] DeactivatePropertyRequest request,
       CancellationToken ct)
        {
            var CurrentUserLogin =
                await GetCurrentuserLogeined();

            if (!CurrentUserLogin.Success ||
                CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }

            var result =
                await _service.DeactivateAsync(
                    expected,
                    CurrentUserLogin.Data,
                    id,
                    request,
                    ct);

            return Result(result);
        }


        [HttpGet("{id:int}/settings")]
        [Authorize(Policy = Permissions.Properties.Read)]
        public async Task<IActionResult> GetSettings(
            int id,
            CancellationToken ct)
        {
            //authentaction check
            var CurrentUserLogin = await GetCurrentuserLogeined();
            if (!CurrentUserLogin.Success || CurrentUserLogin.Data == null)
            {
                return Result(CurrentUserLogin);
            }
            var result = await _service.GetSettingsAsync(CurrentUserLogin.Data,id, ct);
            return Result(result);
        }

        [HttpPut("{id:int}/settings")]
        [Authorize(Policy = Permissions.Properties.Update)]
        public async Task<IActionResult> UpdateSettings(
            int id,string ifmatch,
            UpdatePropertySettingsRequest request,
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
            var result = await _service.UpdateSettingsAsync(CurrentUserLogin.Data,id,ifmatch,request,ct);

            return Result(result);
        }
 
                              
 
     

    }
}

