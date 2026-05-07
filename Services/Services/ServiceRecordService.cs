using AutoStock.Repositories;

using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services.Services;

public class ServiceRecordService : IServiceRecordService
{
    private readonly AppDbContext _context;

    public ServiceRecordService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<CreateServiceRecordResponse>> CreateAsync(
        CreateServiceRecordRequest request,
        int workshopId)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerPhoneNumber))
            return ServiceResult<CreateServiceRecordResponse>.Fail("Telefon numarası zorunludur.");

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return ServiceResult<CreateServiceRecordResponse>.Fail("Müşteri adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Plate))
            return ServiceResult<CreateServiceRecordResponse>.Fail("Plaka zorunludur.");

        var phoneNumber = request.CustomerPhoneNumber.Trim();
        var plate = NormalizePlate(request.Plate);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(x =>
                x.WorkshopId == workshopId &&
                x.PhoneNumber == phoneNumber);

        if (customer is null)
        {
            customer = new Customer
            {
                WorkshopId = workshopId,
                Type = CustomerType.Individual,
                PhoneNumber = phoneNumber,
                FullName = request.CustomerName.Trim()
            };

            _context.Customers.Add(customer);
        }

        var vehicle = await _context.Vehicles
            .Include(x => x.VehicleBrand)
            .Include(x => x.VehicleModel)
            .FirstOrDefaultAsync(x =>
                x.WorkshopId == workshopId &&
                x.Plate == plate);

        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                WorkshopId = workshopId,
                Customer = customer,
                Plate = plate,
                VehicleBrandId = request.VehicleBrandId,
                VehicleModelId = request.VehicleModelId,
                ModelYear = request.ModelYear,
                Mileage = request.Mileage
            };

            _context.Vehicles.Add(vehicle);
        }
        else
        {
            vehicle.Customer = customer;
            vehicle.VehicleBrandId = request.VehicleBrandId ?? vehicle.VehicleBrandId;
            vehicle.VehicleModelId = request.VehicleModelId ?? vehicle.VehicleModelId;
            vehicle.ModelYear = request.ModelYear ?? vehicle.ModelYear;
            vehicle.Mileage = request.Mileage ?? vehicle.Mileage;
        }

        var brandName = await GetBrandNameAsync(request.VehicleBrandId);
        var modelName = await GetModelNameAsync(request.VehicleModelId);

        var serviceRecord = new ServiceRecord
        {
            RecordNumber = GenerateRecordNumber(),
            WorkshopId = workshopId,
            Customer = customer,
            Vehicle = vehicle,
            Status = ServiceRecordStatus.Open,

            CustomerNameSnapshot = request.CustomerName.Trim(),
            CustomerPhoneSnapshot = phoneNumber,

            VehiclePlateSnapshot = plate,
            VehicleBrandNameSnapshot = brandName,
            VehicleModelNameSnapshot = modelName,
            MileageSnapshot = request.Mileage,

            CustomerComplaint = request.CustomerComplaint?.Trim(),
            ServiceReceptionNote = request.ServiceReceptionNote?.Trim(),

            TotalAmount = 0,
            ShowPricesOnPdf = true
        };

        _context.ServiceRecords.Add(serviceRecord);

       

        await _context.SaveChangesAsync();

        return ServiceResult<CreateServiceRecordResponse>.Success(new CreateServiceRecordResponse
        {
            ServiceRecordId = serviceRecord.Id,
            RecordNumber = serviceRecord.RecordNumber
        });
    }

    private async Task<string?> GetBrandNameAsync(int? brandId)
    {
        if (!brandId.HasValue)
            return null;

        return await _context.VehicleBrands
            .Where(x => x.Id == brandId.Value)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }

    private async Task<string?> GetModelNameAsync(int? modelId)
    {
        if (!modelId.HasValue)
            return null;

        return await _context.VehicleModels
            .Where(x => x.Id == modelId.Value)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
    }

    private static string NormalizePlate(string plate)
    {
        return plate
            .Trim()
            .Replace(" ", "")
            .ToUpperInvariant();
    }

    private static string GenerateRecordNumber()
    {
        return $"SR-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }
}