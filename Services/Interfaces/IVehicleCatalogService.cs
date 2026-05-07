using AutoStock.Services.Dtos.Vehicles;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Interfaces
{
    public interface IVehicleCatalogService
    {
        Task<List<VehicleBrandDto>> GetBrandsAsync();

        Task<List<VehicleModelDto>> GetModelsByBrandIdAsync(int brandId);
    }
}
