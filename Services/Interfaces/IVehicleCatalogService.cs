using AutoStock.Services.Dtos.Vehicles;

namespace AutoStock.Services.Interfaces
{
    public interface IVehicleCatalogService
    {
        Task<List<VehicleBrandDto>> GetBrandsAsync();

        Task<List<VehicleModelDto>> GetModelsByBrandIdAsync(int brandId);
    }
}
