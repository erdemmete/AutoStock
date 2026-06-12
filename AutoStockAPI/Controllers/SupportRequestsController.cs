using AutoStock.API.Controllers;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SupportRequests;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    public class SupportRequestsController : BaseApiController
    {
        private readonly ISupportRequestService _supportRequestService;

        public SupportRequestsController(ISupportRequestService supportRequestService)
        {
            _supportRequestService = supportRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] SupportRequestListQueryDto query)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.GetPagedForWorkshopAsync(
                query,
                workshopIdResult.Data,
                userIdResult.Data,
                GetCurrentUserRole());

            return ToActionResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.GetByIdForWorkshopAsync(
                id,
                workshopIdResult.Data,
                userIdResult.Data,
                GetCurrentUserRole());

            return ToActionResult(result);
        }

        [HttpPost("issue")]
        public async Task<IActionResult> CreateIssue(CreateIssueSupportRequestDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.CreateIssueAsync(
                request,
                workshopIdResult.Data,
                userIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("user-create-request")]
        [Authorize(Roles = AppRoles.Owner)]
        public async Task<IActionResult> CreateUserRequest(CreateUserSupportRequestDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.CreateUserCreateRequestAsync(
                request,
                workshopIdResult.Data,
                userIdResult.Data,
                GetCurrentUserRole());

            return ToActionResult(result);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.CancelForWorkshopAsync(
                id,
                workshopIdResult.Data,
                userIdResult.Data);

            return ToActionResult(result);
        }
    }
}