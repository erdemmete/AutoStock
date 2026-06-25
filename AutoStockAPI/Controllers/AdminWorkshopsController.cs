using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/admin/workshops")]
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminWorkshopsController : ControllerBase
    {
        private readonly IAdminWorkshopService _adminWorkshopService;
        private readonly IEntityEditLockService _entityEditLockService;

        public AdminWorkshopsController(
            IAdminWorkshopService adminWorkshopService,
            IEntityEditLockService entityEditLockService)
        {
            _adminWorkshopService = adminWorkshopService;
            _entityEditLockService = entityEditLockService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] AdminWorkshopListQueryDto query)
        {
            var result = await _adminWorkshopService.GetPagedAsync(query);

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

        [HttpGet("{workshopId:int}/edit-locks")]
        public async Task<IActionResult> GetEditLocks(int workshopId)
        {
            var result = await _entityEditLockService.GetWorkshopLocksForAdminAsync(workshopId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{workshopId:int}/edit-locks/{entityType}/{entityId:int}")]
        public async Task<IActionResult> ForceReleaseEditLock(
            int workshopId,
            string entityType,
            int entityId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var adminUserId))
                return Unauthorized();

            var result = await _entityEditLockService.ForceReleaseForAdminAsync(
                entityType,
                entityId,
                workshopId,
                adminUserId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{workshopId:int}/users/{userId:int}")]
        public async Task<IActionResult> GetUserDetail(int workshopId, int userId)
        {
            var result = await _adminWorkshopService.GetUserDetailAsync(workshopId, userId);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{workshopId:int}/users")]
        public async Task<IActionResult> CreateUser(int workshopId, CreateAdminWorkshopUserRequestDto request)
        {
            var result = await _adminWorkshopService.CreateUserAsync(workshopId, request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{workshopId:int}/users/{userId:int}")]
        public async Task<IActionResult> UpdateUser(int workshopId, int userId, UpdateAdminWorkshopUserRequestDto request)
        {
            var result = await _adminWorkshopService.UpdateUserAsync(workshopId, userId, request);

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

        [HttpPost("{workshopId:int}/users/{userId:int}/password-reset-link")]
        public async Task<IActionResult> CreateUserPasswordResetLink(int workshopId, int userId)
        {
            var result = await _adminWorkshopService.CreateUserPasswordResetLinkAsync(
                workshopId,
                userId);

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

        [HttpGet("{workshopId:int}/bank-accounts")]
        public async Task<IActionResult> GetBankAccounts(int workshopId)
        {
            var result = await _adminWorkshopService.GetBankAccountsAsync(workshopId);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{workshopId:int}/bank-accounts")]
        public async Task<IActionResult> CreateBankAccount(
            int workshopId,
            CreateAdminWorkshopBankAccountRequestDto request)
        {
            var result = await _adminWorkshopService.CreateBankAccountAsync(
                workshopId,
                request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{workshopId:int}/bank-accounts/{bankAccountId:int}")]
        public async Task<IActionResult> UpdateBankAccount(
            int workshopId,
            int bankAccountId,
            UpdateAdminWorkshopBankAccountRequestDto request)
        {
            var result = await _adminWorkshopService.UpdateBankAccountAsync(
                workshopId,
                bankAccountId,
                request);

            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{workshopId:int}/bank-accounts/{bankAccountId:int}")]
        public async Task<IActionResult> DeleteBankAccount(int workshopId, int bankAccountId)
        {
            var result = await _adminWorkshopService.DeleteBankAccountAsync(
                workshopId,
                bankAccountId);

            return StatusCode((int)result.StatusCode, result);
        }

    }
}
