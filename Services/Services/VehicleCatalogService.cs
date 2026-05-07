using AutoStock.Repositories;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Services
{
    public class VehicleCatalogService : IVehicleCatalogService
    {
        private readonly AppDbContext _context;

        public VehicleCatalogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleBrandDto>> GetBrandsAsync()
        {
            return await _context.VehicleBrands
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new VehicleBrandDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<VehicleModelDto>> GetModelsByBrandIdAsync(int brandId)
        {
            return await _context.VehicleModels
                .Where(x => x.IsActive && x.VehicleBrandId == brandId)
                .OrderBy(x => x.Name)
                .Select(x => new VehicleModelDto
                {
                    Id = x.Id,
                    VehicleBrandId = x.VehicleBrandId,
                    Name = x.Name
                })
                .ToListAsync();
        }
    }
}
