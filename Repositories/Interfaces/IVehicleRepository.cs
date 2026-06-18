using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories.Interfaces
{
    public interface IVehicleRepository
    {
        Task<List<Vehicle>> SearchByPlateAsync(string plate, int workshopId);

        Task<Vehicle?> GetByIdAsync(int vehicleId, int workshopId);
    }
}
