using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VehicleCatalogController : ControllerBase
    {
        private readonly IVehicleCatalogService _vehicleCatalogService;

        public VehicleCatalogController(IVehicleCatalogService vehicleCatalogService)
        {
            _vehicleCatalogService = vehicleCatalogService;
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _vehicleCatalogService.GetBrandsAsync();

            return OkServiceResult(brands);
        }

        [HttpGet("brands/{brandId:int}/models")]
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var models = await _vehicleCatalogService.GetModelsByBrandIdAsync(brandId);

            return OkServiceResult(models);
        }

        private IActionResult OkServiceResult<T>(T data)
        {
            return Ok(ServiceResult<T>.Success(data));
        }
    }
}