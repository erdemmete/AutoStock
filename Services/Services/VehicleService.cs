using AutoStock.Repositories.Interfaces;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Services.Interfaces;
using System.Net;

namespace AutoStock.Services.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository vehicleRepository;

        public VehicleService(IVehicleRepository vehicleRepository)
        {
            this.vehicleRepository = vehicleRepository;
        }

        public async Task<ServiceResult<List<VehicleSearchDto>>> SearchByPlateAsync(
            string plate,
            int workshopId)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return ServiceResult<List<VehicleSearchDto>>.Success(new List<VehicleSearchDto>());

            var vehicles = await vehicleRepository.SearchByPlateAsync(plate, workshopId);

            var result = vehicles
                .Where(x => x.IsActive && x.Customer?.IsActive != false)
                .Select(MapToSearchDto)
                .ToList();

            return ServiceResult<List<VehicleSearchDto>>.Success(result);
        }

        public async Task<ServiceResult<VehicleSearchDto>> GetByIdAsync(int vehicleId, int workshopId)
        {
            if (vehicleId <= 0)
            {
                return ServiceResult<VehicleSearchDto>.Fail(
                    "Araç bilgisi geçersiz.",
                    HttpStatusCode.BadRequest);
            }

            var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, workshopId);

            if (vehicle == null)
            {
                return ServiceResult<VehicleSearchDto>.Fail(
                    "Araç bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            if (!vehicle.IsActive)
            {
                return ServiceResult<VehicleSearchDto>.Fail(
                    "Araç kaydı aktif değil.",
                    HttpStatusCode.NotFound);
            }

            if (vehicle.Customer == null || !vehicle.Customer.IsActive)
            {
                return ServiceResult<VehicleSearchDto>.Fail(
                    "Araca bağlı aktif müşteri bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            return ServiceResult<VehicleSearchDto>.Success(MapToSearchDto(vehicle));
        }

        private static VehicleSearchDto MapToSearchDto(Repositories.Entities.Vehicle vehicle)
        {
            var customerName = vehicle.Customer == null
                ? null
                : !string.IsNullOrWhiteSpace(vehicle.Customer.FullName)
                    ? vehicle.Customer.FullName
                    : vehicle.Customer.CompanyName;

            return new VehicleSearchDto
            {
                Id = vehicle.Id,

                Plate = vehicle.Plate,

                CustomerId = vehicle.CustomerId,
                CustomerType = vehicle.Customer == null ? null : (int)vehicle.Customer.Type,

                CustomerName = customerName,
                CustomerPhone = vehicle.Customer?.PhoneNumber,
                CustomerEmail = vehicle.Customer?.Email,

                CompanyName = vehicle.Customer?.CompanyName,
                AuthorizedPersonName = vehicle.Customer?.AuthorizedPersonName,
                NationalIdentityNumber = vehicle.Customer?.NationalIdentityNumber,
                TaxOffice = vehicle.Customer?.TaxOffice,
                TaxNumber = vehicle.Customer?.TaxNumber,
                AddressCity = vehicle.Customer?.AddressCity,
                AddressDistrict = vehicle.Customer?.AddressDistrict,
                CustomerAddress = vehicle.Customer?.Address,

                BrandId = vehicle.VehicleBrandId,
                VehicleBrandId = vehicle.VehicleBrandId,
                BrandName = vehicle.VehicleBrand?.Name,

                ModelId = vehicle.VehicleModelId,
                VehicleModelId = vehicle.VehicleModelId,
                ModelName = vehicle.VehicleModel?.Name,

                ModelYear = vehicle.ModelYear,
                ChassisNumber = vehicle.ChassisNumber,
                VehicleVariantId = vehicle.VehicleVariantId,
                VehicleVariantName = vehicle.VehicleVariant?.Name,
                FuelType = vehicle.FuelType,
                TransmissionType = vehicle.TransmissionType,
                BodyType = vehicle.BodyType,
                EngineCapacityCc = vehicle.EngineCapacityCc,
                EnginePowerHp = vehicle.EnginePowerHp,
                EngineCode = vehicle.EngineCode
            };
        }
    }
}
