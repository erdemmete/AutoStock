using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Owner + "," + AppRoles.Staff)]
    public class VehicleCatalogController : BaseApiController
    {
        private readonly IVehicleCatalogService _vehicleCatalogService;
        private readonly IVehicleCatalogSeeder _vehicleCatalogSeeder;

        public VehicleCatalogController(IVehicleCatalogService vehicleCatalogService, IVehicleCatalogSeeder vehicleCatalogSeeder)
        {
            _vehicleCatalogService = vehicleCatalogService;
            _vehicleCatalogSeeder = vehicleCatalogSeeder;
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _vehicleCatalogService.GetBrandsAsync();

            return ToActionResult(ServiceResult<object>.Success(brands));
        }

        [HttpGet("brands/{brandId:int}/models")]
        public async Task<IActionResult> GetModelsByBrandId(int brandId)
        {
            var models = await _vehicleCatalogService.GetModelsByBrandIdAsync(brandId);

            return ToActionResult(ServiceResult<object>.Success(models));
        }

        [HttpGet("models/{modelId:int}/variants")]
        public async Task<IActionResult> GetVariantsByModelId(int modelId)
        {
            var variants = await _vehicleCatalogService.GetVariantsByModelIdAsync(modelId);

            return ToActionResult(ServiceResult<object>.Success(variants));
        }

        [HttpPost("seed")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Seed()
        {
            var result = await _vehicleCatalogSeeder.SeedAsync();

            return ToActionResult(result);
        }
    }
}