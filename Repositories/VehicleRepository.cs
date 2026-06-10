using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly AppDbContext context;

        public VehicleRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Vehicle>> SearchByPlateAsync(string plate, int workshopId)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return new List<Vehicle>();

            var normalizedPlate = plate
                .Replace(" ", "")
                .Trim()
                .ToUpper();

            return await context.Set<Vehicle>()
                .Include(x => x.Customer)
                .Include(x => x.VehicleBrand)
                .Include(x => x.VehicleModel)
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Plate.Replace(" ", "").ToUpper().Contains(normalizedPlate))
                .OrderBy(x => x.Plate)
                .Take(10)
                .ToListAsync();
        }
    }
}