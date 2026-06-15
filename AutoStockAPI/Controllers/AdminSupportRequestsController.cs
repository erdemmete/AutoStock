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
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminSupportRequestsController : BaseApiController
    {
        private readonly ISupportRequestService _supportRequestService;

        public AdminSupportRequestsController(ISupportRequestService supportRequestService)
        {
            _supportRequestService = supportRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] AdminSupportRequestListQueryDto query)
        {
            var result = await _supportRequestService.GetPagedForAdminAsync(query);
            return ToActionResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _supportRequestService.GetByIdForAdminAsync(id);
            return ToActionResult(result);
        }

        [HttpPost("{id:int}/answer")]
        public async Task<IActionResult> Answer(int id, AdminAnswerSupportRequestDto request)
        {
            if (id != request.Id)
            {
                return ToActionResult(ServiceResult<int>.Fail(
                    "Destek talebi bilgisi hatalı.",
                    HttpStatusCode.BadRequest));
            }

            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure) return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.AnswerAsync(
                request,
                userIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, AdminUpdateSupportRequestStatusDto request)
        {
            if (id != request.Id)
            {
                return ToActionResult(ServiceResult<int>.Fail(
                    "Destek talebi bilgisi hatalı.",
                    HttpStatusCode.BadRequest));
            }

            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure) return UnauthorizedResult(userIdResult);

            var result = await _supportRequestService.UpdateStatusAsync(
                request,
                userIdResult.Data);

            return ToActionResult(result);
        }
    }
}
