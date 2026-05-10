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
                FullName = request.CustomerName.Trim(),
                Email = request.CustomerEmail?.Trim()
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
            EstimatedAmount = request.EstimatedAmount,
            EstimatedAmountNote = request.EstimatedAmountNote?.Trim(),
            TotalAmount = 0,
            ShowPricesOnPdf = true
        };

        foreach (var item in request.RequestItems.Where(x => !string.IsNullOrWhiteSpace(x.Title)))
        {
            serviceRecord.RequestItems.Add(new ServiceRequestItem
            {
                Title = item.Title.Trim(),
                Note = item.Note?.Trim(),
                EstimatedAmount = item.EstimatedAmount
            });
        }

        _context.ServiceRecords.Add(serviceRecord);

       

        await _context.SaveChangesAsync();

        return ServiceResult<CreateServiceRecordResponse>.Success(new CreateServiceRecordResponse
        {
            ServiceRecordId = serviceRecord.Id,
            RecordNumber = serviceRecord.RecordNumber
        });
    }

    public async Task<ServiceResult<ServiceRecordDetailDto>> GetDetailAsync(
    int serviceRecordId,
    int workshopId)
    {
        var serviceRecord = await _context.ServiceRecords
            .Include(x => x.Operations)
            .Include(x => x.RequestItems)
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
        {
            return ServiceResult<ServiceRecordDetailDto>
                .Fail("Servis kaydı bulunamadı.");
        }

        var dto = new ServiceRecordDetailDto
        {
            Id = serviceRecord.Id,
            RecordNumber = serviceRecord.RecordNumber,
            Status = serviceRecord.Status,

            CustomerName = serviceRecord.CustomerNameSnapshot,
            CustomerPhone = serviceRecord.CustomerPhoneSnapshot,

            VehiclePlate = serviceRecord.VehiclePlateSnapshot,
            VehicleBrandName = serviceRecord.VehicleBrandNameSnapshot,
            VehicleModelName = serviceRecord.VehicleModelNameSnapshot,

            Mileage = serviceRecord.MileageSnapshot,

            CustomerComplaint = serviceRecord.CustomerComplaint,
            ServiceReceptionNote = serviceRecord.ServiceReceptionNote,
            RepairNote = serviceRecord.RepairNote,

            TotalAmount = serviceRecord.TotalAmount,

            CreatedAt = serviceRecord.CreatedAt,
            CompletedAt = serviceRecord.CompletedAt,

            Operations = serviceRecord.Operations
                .OrderBy(x => x.Id)
                .Select(x => new ServiceOperationDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.TotalPrice,
                    Note = x.Note,
                    ServiceRequestItemId = x.ServiceRequestItemId,
                })
                .ToList(),

            RequestItems = serviceRecord.RequestItems
            .OrderBy(x => x.Id)
            .Select(x => new ServiceRequestItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Note = x.Note,
                RepairDetail = x.RepairDetail,
                EstimatedAmount = x.EstimatedAmount,
                FinalAmount = x.FinalAmount,
                IsResolved = x.IsResolved
            })
            .ToList()

        };

        return ServiceResult<ServiceRecordDetailDto>.Success(dto);
    }

    public async Task<ServiceResult<List<ServiceRecordListItemDto>>> GetListAsync(int workshopId)
    {
        var records = await _context.ServiceRecords
            .Where(x => x.WorkshopId == workshopId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ServiceRecordListItemDto
            {
                Id = x.Id,
                RecordNumber = x.RecordNumber,
                Status = x.Status,

                CustomerName = x.CustomerNameSnapshot,
                CustomerPhone = x.CustomerPhoneSnapshot,

                VehiclePlate = x.VehiclePlateSnapshot,
                VehicleBrandName = x.VehicleBrandNameSnapshot,
                VehicleModelName = x.VehicleModelNameSnapshot,

                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return ServiceResult<List<ServiceRecordListItemDto>>.Success(records);
    }

    public async Task<ServiceResult<bool>> UpdateRequestItemAsync(
    int requestItemId,
    UpdateServiceRequestItemRequest request,
    int workshopId)
    {
        var requestItem = await _context.ServiceRequestItems
            .Include(x => x.ServiceRecord)
            .FirstOrDefaultAsync(x =>
                x.Id == requestItemId &&
                x.ServiceRecord.WorkshopId == workshopId);

        if (requestItem is null)
            return ServiceResult<bool>.Fail("Talep kaydı bulunamadı.");

        requestItem.RepairDetail = request.RepairDetail?.Trim();
        requestItem.FinalAmount = request.FinalAmount;
        requestItem.IsResolved = request.IsResolved;

        requestItem.ServiceRecord.TotalAmount = await _context.ServiceRequestItems
            .Where(x =>
                x.ServiceRecordId == requestItem.ServiceRecordId &&
                x.Id != requestItem.Id)
            .SumAsync(x => x.FinalAmount ?? 0);

        requestItem.ServiceRecord.TotalAmount += request.FinalAmount ?? 0;

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> AddRequestItemAsync(int serviceRecordId, CreateServiceRequestItemDto request, int workshopId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ServiceResult<bool>.Fail("Talep başlığı zorunludur.");

        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

        serviceRecord.RequestItems.Add(new ServiceRequestItem
        {
            Title = request.Title.Trim(),
            Note = request.Note?.Trim(),
            EstimatedAmount = request.EstimatedAmount
        });

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> AddOperationAsync(
    int serviceRecordId,
    AddServiceOperationRequest request,
    int workshopId)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return ServiceResult<bool>.Fail("İşlem açıklaması zorunludur.");

        if (request.Quantity <= 0)
            return ServiceResult<bool>.Fail("Adet 1 veya daha büyük olmalıdır.");

        if (request.UnitPrice < 0)
            return ServiceResult<bool>.Fail("Birim fiyat negatif olamaz.");

        var serviceRecord = await _context.ServiceRecords
            .Include(x => x.Operations)
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

        if (request.ServiceRequestItemId.HasValue)
        {
            var requestItemExists = await _context.ServiceRequestItems
                .AnyAsync(x =>
                    x.Id == request.ServiceRequestItemId.Value &&
                    x.ServiceRecordId == serviceRecordId);

            if (!requestItemExists)
                return ServiceResult<bool>.Fail("Seçilen talep bu servis kaydına ait değil.");
        }

        var totalPrice = request.Quantity * request.UnitPrice;

        serviceRecord.Operations.Add(new ServiceOperation
        {
            ServiceRequestItemId = request.ServiceRequestItemId,
            Type = request.Type,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            TotalPrice = totalPrice,
            Note = request.Note?.Trim()
        });

        serviceRecord.TotalAmount = serviceRecord.Operations.Sum(x => x.TotalPrice) + totalPrice;

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
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