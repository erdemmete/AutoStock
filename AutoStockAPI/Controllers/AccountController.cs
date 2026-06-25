using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Account;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.OwnerOrStaff)]
    public class AccountController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _accountService.GetOverviewAsync(userIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPut("email")]
        public async Task<IActionResult> UpdateEmail(UpdateAccountEmailRequestDto request)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _accountService.UpdateEmailAsync(userIdResult.Data, request);

            return ToActionResult(result);
        }

        [HttpPost("email-confirmation/request")]
        public async Task<IActionResult> RequestEmailConfirmation(RequestEmailConfirmationDto request)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _accountService.SendEmailConfirmationAsync(
                userIdResult.Data,
                request);

            return ToActionResult(result);
        }

        [AllowAnonymous]
        [HttpPost("email-confirmation/confirm")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto request)
        {
            var result = await _accountService.ConfirmEmailAsync(request);
            return ToActionResult(result);
        }

        [HttpPut("phone")]
        public async Task<IActionResult> UpdatePhone(UpdateAccountPhoneRequestDto request)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _accountService.UpdatePhoneAsync(userIdResult.Data, request);

            return ToActionResult(result);
        }

        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _accountService.ChangePasswordAsync(userIdResult.Data, request);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpGet("workshop")]
        public async Task<IActionResult> GetWorkshopProfile()
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.GetWorkshopProfileAsync(
                userIdResult.Data,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpPut("workshop")]
        public async Task<IActionResult> UpdateWorkshopProfile(UpdateWorkshopProfileManagementRequestDto request)
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.UpdateWorkshopProfileAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                request);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpPost("workshop/bank-accounts")]
        public async Task<IActionResult> CreateWorkshopBankAccount(CreateAccountWorkshopBankAccountRequestDto request)
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.CreateWorkshopBankAccountAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                request);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpPut("workshop/bank-accounts/{bankAccountId:int}")]
        public async Task<IActionResult> UpdateWorkshopBankAccount(
            int bankAccountId,
            UpdateAccountWorkshopBankAccountRequestDto request)
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.UpdateWorkshopBankAccountAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                bankAccountId,
                request);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpDelete("workshop/bank-accounts/{bankAccountId:int}")]
        public async Task<IActionResult> DeleteWorkshopBankAccount(int bankAccountId)
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.DeleteWorkshopBankAccountAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                bankAccountId);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpGet("membership")]
        public async Task<IActionResult> GetMembership()
        {
            var userIdResult = GetCurrentUserId();
            var workshopIdResult = GetCurrentWorkshopId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountService.GetMembershipAsync(
                userIdResult.Data,
                workshopIdResult.Data);

            return ToActionResult(result);
        }
    }
}
