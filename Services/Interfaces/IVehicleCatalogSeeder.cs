using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Vehicles;

namespace AutoStock.Services.Interfaces
{
    public interface IVehicleCatalogSeeder
    {
        Task<ServiceResult<VehicleCatalogSeedResultDto>> SeedAsync(CancellationToken cancellationToken = default);
    }
}
