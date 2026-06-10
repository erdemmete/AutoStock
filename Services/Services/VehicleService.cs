using AutoStock.Repositories.Interfaces;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Services.Interfaces;

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

            var result = vehicles.Select(x =>
            {
                var customerName = x.Customer == null
                    ? null
                    : !string.IsNullOrWhiteSpace(x.Customer.FullName)
                        ? x.Customer.FullName
                        : x.Customer.CompanyName;

                return new VehicleSearchDto
                {
                    Id = x.Id,

                    Plate = x.Plate,

                    CustomerId = x.CustomerId,
                    CustomerType = x.Customer == null ? null : (int)x.Customer.Type,

                    CustomerName = customerName,
                    CustomerPhone = x.Customer?.PhoneNumber,
                    CustomerEmail = x.Customer?.Email,

                    CompanyName = x.Customer?.CompanyName,
                    AuthorizedPersonName = x.Customer?.AuthorizedPersonName,
                    NationalIdentityNumber = x.Customer?.NationalIdentityNumber,
                    TaxOffice = x.Customer?.TaxOffice,
                    TaxNumber = x.Customer?.TaxNumber,
                    AddressCity = x.Customer?.AddressCity,
                    AddressDistrict = x.Customer?.AddressDistrict,
                    CustomerAddress = x.Customer?.Address,

                    BrandId = x.VehicleBrandId,
                    VehicleBrandId = x.VehicleBrandId,
                    BrandName = x.VehicleBrand?.Name,

                    ModelId = x.VehicleModelId,
                    VehicleModelId = x.VehicleModelId,
                    ModelName = x.VehicleModel?.Name,

                    ModelYear = x.ModelYear,
                    ChassisNumber = x.ChassisNumber
                };
            }).ToList();

            return ServiceResult<List<VehicleSearchDto>>.Success(result);
        }
    }
}