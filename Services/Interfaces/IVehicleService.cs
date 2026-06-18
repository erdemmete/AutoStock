using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Vehicles;

namespace AutoStock.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<ServiceResult<List<VehicleSearchDto>>> SearchByPlateAsync(string plate, int workshopId);

        Task<ServiceResult<VehicleSearchDto>> GetByIdAsync(int vehicleId, int workshopId);
    }
}
