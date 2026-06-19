using AutoStock.API.Controllers;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStockAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/service-record-images")]
    public class ServiceRecordImagesController : BaseApiController
    {
        private readonly IServiceRecordImageService _serviceRecordImageService;
        private readonly IEntityEditLockService _entityEditLockService;

        public ServiceRecordImagesController(
            IServiceRecordImageService serviceRecordImageService,
            IEntityEditLockService entityEditLockService)
        {
            _serviceRecordImageService = serviceRecordImageService;
            _entityEditLockService = entityEditLockService;
        }

        [HttpGet("service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetByServiceRecord(int serviceRecordId)
        {
            var workshopResult = GetCurrentWorkshopId();

            if (workshopResult.IsFailure || workshopResult.Data <= 0)
                return ToActionResult(workshopResult);

            var result = await _serviceRecordImageService.GetByServiceRecordAsync(
                workshopResult.Data,
                serviceRecordId);

            return ToActionResult(result);
        }

        [HttpPost("{serviceRecordId:int}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Upload(
            int serviceRecordId,
            [FromForm] IFormFile file,
            [FromForm] ServiceImageType type = ServiceImageType.BeforeRepair,
            [FromForm] string? description = null)
        {
            var workshopResult = GetCurrentWorkshopId();

            if (workshopResult.IsFailure || workshopResult.Data <= 0)
                return ToActionResult(workshopResult);

            var lockResult = await ValidateServiceRecordLockAsync(serviceRecordId, workshopResult.Data);
            if (lockResult is not null)
                return lockResult;

            if (file is null || file.Length <= 0)
                return BadRequest("Fotoğraf dosyası zorunludur.");

            await using var stream = file.OpenReadStream();

            var result = await _serviceRecordImageService.UploadAsync(
                workshopResult.Data,
                serviceRecordId,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                type,
                description);

            return ToActionResult(result);
        }

        [HttpGet("{imageId:int}/content")]
        public async Task<IActionResult> GetContent(int imageId)
        {
            var workshopResult = GetCurrentWorkshopId();

            if (workshopResult.IsFailure || workshopResult.Data <= 0)
                return ToActionResult(workshopResult);

            var result = await _serviceRecordImageService.GetContentAsync(
                workshopResult.Data,
                imageId);

            if (result.IsFailure || result.Data is null)
                return ToActionResult(result);

            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpDelete("{imageId:int}")]
        public async Task<IActionResult> Delete(int imageId)
        {
            var workshopResult = GetCurrentWorkshopId();

            if (workshopResult.IsFailure || workshopResult.Data <= 0)
                return ToActionResult(workshopResult);

            var lockResult = await ValidateImageLockAsync(imageId, workshopResult.Data);
            if (lockResult is not null)
                return lockResult;

            var result = await _serviceRecordImageService.DeleteAsync(
                workshopResult.Data,
                imageId);

            return ToActionResult(result);
        }

        private async Task<IActionResult?> ValidateServiceRecordLockAsync(int serviceRecordId, int workshopId)
        {
            var userResult = GetCurrentUserId();
            if (userResult.IsFailure)
                return ToActionResult(userResult);

            var result = await _entityEditLockService.ValidateAsync(
                "ServiceRecord",
                serviceRecordId,
                GetEditLockToken(),
                workshopId,
                userResult.Data);

            return result.IsFailure ? ToActionResult(result) : null;
        }

        private async Task<IActionResult?> ValidateImageLockAsync(int imageId, int workshopId)
        {
            var userResult = GetCurrentUserId();
            if (userResult.IsFailure)
                return ToActionResult(userResult);

            var result = await _entityEditLockService.ValidateServiceRecordImageAsync(
                imageId,
                GetEditLockToken(),
                workshopId,
                userResult.Data);

            return result.IsFailure ? ToActionResult(result) : null;
        }
    }
}
