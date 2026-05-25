using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/admin/workshops")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminWorkshopsController : ControllerBase
    {
        private readonly IAdminWorkshopService _adminWorkshopService;

        public AdminWorkshopsController(IAdminWorkshopService adminWorkshopService)
        {
            _adminWorkshopService = adminWorkshopService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var result = await _adminWorkshopService.GetListAsync();

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _adminWorkshopService.GetByIdAsync(id);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAdminWorkshopRequestDto request)
        {
            var result = await _adminWorkshopService.CreateAsync(request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:int}/subscription")]
        public async Task<IActionResult> UpdateSubscription(int id, UpdateAdminWorkshopSubscriptionRequestDto request)
        {
            var result = await _adminWorkshopService.UpdateSubscriptionAsync(id, request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{workshopId:int}/users")]
        public async Task<IActionResult> GetUsers(int workshopId)
        {
            var result = await _adminWorkshopService.GetUsersAsync(workshopId);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{workshopId:int}/users")]
        public async Task<IActionResult> CreateUser(int workshopId, CreateAdminWorkshopUserRequestDto request)
        {
            var result = await _adminWorkshopService.CreateUserAsync(workshopId, request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{workshopId:int}/users/{userId:int}/status")]
        public async Task<IActionResult> UpdateUserStatus(int workshopId, int userId, UpdateAdminWorkshopUserStatusRequestDto request)
        {
            var result = await _adminWorkshopService.UpdateUserStatusAsync(
                workshopId,
                userId,
                request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:int}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, UpdateAdminWorkshopProfileRequestDto request)
        {
            var result = await _adminWorkshopService.UpdateProfileAsync(id, request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{workshopId:int}/partners")]
        public async Task<IActionResult> GetPartners(int workshopId)
        {
            var result = await _adminWorkshopService.GetPartnersAsync(workshopId);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{workshopId:int}/partners")]
        public async Task<IActionResult> CreatePartner(int workshopId, CreateAdminWorkshopPartnerRequestDto request)
        {
            var result = await _adminWorkshopService.CreatePartnerAsync(
                workshopId,
                request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{workshopId:int}/partners/{partnerId:int}")]
        public async Task<IActionResult> UpdatePartner(int workshopId, int partnerId, UpdateAdminWorkshopPartnerRequestDto request)
        {
            var result = await _adminWorkshopService.UpdatePartnerAsync(
                workshopId,
                partnerId,
                request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{workshopId:int}/partners/{partnerId:int}")]
        public async Task<IActionResult> DeletePartner(int workshopId, int partnerId)
        {
            var result = await _adminWorkshopService.DeletePartnerAsync(
                workshopId,
                partnerId);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{workshopId:int}/users/suggest-credentials")]
        public async Task<IActionResult> SuggestUserCredentials(int workshopId, [FromQuery] string fullName)
        {
            var result = await _adminWorkshopService.SuggestUserCredentialsAsync(
                workshopId,
                fullName);

            return StatusCode((int)result.StatusCode, result);
        }
    }
}