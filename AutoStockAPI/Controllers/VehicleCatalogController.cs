using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

            return Ok(brands);
        }

        [HttpGet("brands/{brandId:int}/models")]
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var models = await _vehicleCatalogService.GetModelsByBrandIdAsync(brandId);

            return Ok(models);
        }
    }
}