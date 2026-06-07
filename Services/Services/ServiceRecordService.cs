using AutoStock.Repositories;

using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.StockItems;

namespace AutoStock.Services.Services;

public class ServiceRecordService : IServiceRecordService
{
    private readonly AppDbContext _context;
    private readonly IStockItemService _stockItemService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    public ServiceRecordService(
        AppDbContext context,
        IStockItemService stockItemService,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService)
    {
        _context = context;
        _stockItemService = stockItemService;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    public async Task<ServiceResult<CreateServiceRecordResponse>> CreateAsync(CreateServiceRecordRequest request,int workshopId)
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
                Type = request.CustomerType,
                PhoneNumber = phoneNumber,
                FullName = request.CustomerName.Trim(),
                Email = request.CustomerEmail?.Trim(),
                CompanyName = request.CompanyName?.Trim(),
                AuthorizedPersonName = request.AuthorizedPersonName?.Trim(),
                TaxNumber = request.TaxNumber?.Trim(),
                TaxOffice = request.TaxOffice?.Trim(),
                Address = request.CustomerAddress?.Trim(),
                IsActive = true,
                NationalIdentityNumber = request.NationalIdentityNumber?.Trim(),
                AddressCity = request.AddressCity?.Trim(),
                AddressDistrict = request.AddressDistrict?.Trim(),

            };

            _context.Customers.Add(customer);
        }

        else
        {
            customer.Type = request.CustomerType;
            customer.FullName = request.CustomerName.Trim();
            customer.Email = request.CustomerEmail?.Trim();

            customer.CompanyName = request.CompanyName?.Trim();
            customer.AuthorizedPersonName = request.AuthorizedPersonName?.Trim();
            customer.TaxNumber = request.TaxNumber?.Trim();
            customer.TaxOffice = request.TaxOffice?.Trim();
            customer.Address = request.CustomerAddress?.Trim();
            customer.IsActive = true;
            customer.NationalIdentityNumber = request.NationalIdentityNumber?.Trim();
            customer.AddressCity = request.AddressCity?.Trim();
            customer.AddressDistrict = request.AddressDistrict?.Trim();
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
                Mileage = request.Mileage,
                ChassisNumber = request.ChassisNumber?.Trim()
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

            if (!string.IsNullOrWhiteSpace(request.ChassisNumber))
                vehicle.ChassisNumber = request.ChassisNumber.Trim();
        }

        var brandName = await GetBrandNameAsync(request.VehicleBrandId);
        var modelName = await GetModelNameAsync(request.VehicleModelId);

        var vehicleDeliveredBy =
    string.IsNullOrWhiteSpace(request.VehicleDeliveredBy)
        ? request.CustomerName.Trim()
        : request.VehicleDeliveredBy.Trim();

        if (request.CustomerType == CustomerType.Corporate &&
            string.IsNullOrWhiteSpace(request.VehicleDeliveredBy))
        {
            return ServiceResult<CreateServiceRecordResponse>
                .Fail("Kurumsal müşterilerde aracı getiren / ilgili kişi zorunludur.");
        }

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
            VehicleDeliveredBySnapshot = vehicleDeliveredBy,

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

        await _auditLogService.AddAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            ActionType = AuditActionType.Create,
            EntityType = AuditEntityType.ServiceRecord,
            EntityId = serviceRecord.Id,
            Description = $"Servis kaydı oluşturuldu: {GetServiceRecordDisplayName(serviceRecord)}",
            NewValues = new
            {
                serviceRecord.RecordNumber,
                serviceRecord.VehiclePlateSnapshot,
                serviceRecord.CustomerNameSnapshot,
                serviceRecord.Status,
                serviceRecord.EstimatedAmount
            }
        });

        await _context.SaveChangesAsync();

        return ServiceResult<CreateServiceRecordResponse>.Success(new CreateServiceRecordResponse
        {
            ServiceRecordId = serviceRecord.Id,
            RecordNumber = serviceRecord.RecordNumber
        });
    }


    public async Task<ServiceResult<ServiceRecordDetailDto>> GetDetailAsync(int serviceRecordId, int workshopId)
    {
        var serviceRecord = await _context.ServiceRecords
    .Include(x => x.Customer)
    .Include(x => x.Vehicle)
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
            VehicleId = serviceRecord.VehicleId,
            Status = serviceRecord.Status,

            CustomerName = serviceRecord.CustomerNameSnapshot,
            CustomerPhone = serviceRecord.CustomerPhoneSnapshot,
            VehicleDeliveredBy = serviceRecord.VehicleDeliveredBySnapshot,

            CustomerEmail = serviceRecord.Customer.Email,
            CompanyName = serviceRecord.Customer.CompanyName,
            AuthorizedPersonName = serviceRecord.Customer.AuthorizedPersonName,
            TaxNumber = serviceRecord.Customer.TaxNumber,
            TaxOffice = serviceRecord.Customer.TaxOffice,
            NationalIdentityNumber = serviceRecord.Customer.NationalIdentityNumber,
            AddressCity = serviceRecord.Customer.AddressCity,
            AddressDistrict = serviceRecord.Customer.AddressDistrict,
            CustomerAddress = serviceRecord.Customer.Address,

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
            ChassisNumber = serviceRecord.Vehicle.ChassisNumber,
            UpdatedAt = serviceRecord.UpdatedAt,

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
                    StockItemId = x.StockItemId
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

    public async Task<ServiceResult<bool>> UpdateRequestItemAsync(int requestItemId, UpdateServiceRequestItemRequest request, int workshopId)
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

    public async Task<ServiceResult<int>> AddRequestItemAsync(int serviceRecordId, CreateServiceRequestItemDto request, int workshopId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ServiceResult<int>.Fail("Talep başlığı zorunludur.");

        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<int>.Fail("Servis kaydı bulunamadı.");

        var requestItem = new ServiceRequestItem
        {
            Title = request.Title.Trim(),
            Note = request.Note?.Trim(),
            EstimatedAmount = request.EstimatedAmount
        };

        serviceRecord.RequestItems.Add(requestItem);

        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        await _context.SaveChangesAsync();

        return ServiceResult<int>.Success(requestItem.Id);
    }

    public async Task<ServiceResult<ServiceOperationDto>> AddOperationAsync(int serviceRecordId, AddServiceOperationRequest request, int workshopId)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return ServiceResult<ServiceOperationDto>.Fail("İşlem açıklaması zorunludur.");

        if (request.Quantity <= 0)
            return ServiceResult<ServiceOperationDto>.Fail("Adet 1 veya daha büyük olmalıdır.");

        if (request.UnitPrice < 0)
            return ServiceResult<ServiceOperationDto>.Fail("Birim fiyat negatif olamaz.");

        var serviceRecord = await _context.ServiceRecords
            .Include(x => x.Operations)
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<ServiceOperationDto>.Fail("Servis kaydı bulunamadı.");

        if (request.ServiceRequestItemId.HasValue)
        {
            var requestItemExists = await _context.ServiceRequestItems
                .AnyAsync(x =>
                    x.Id == request.ServiceRequestItemId.Value &&
                    x.ServiceRecordId == serviceRecordId);

            if (!requestItemExists)
                return ServiceResult<ServiceOperationDto>.Fail("Seçilen talep bu servis kaydına ait değil.");
        }

        var totalPrice = request.Quantity * request.UnitPrice;

        if (request.Type != OperationType.Part)
        {
            request.StockItemId = null;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var operation = new ServiceOperation
        {
            ServiceRequestItemId = request.ServiceRequestItemId,
            Type = request.Type,
            Description = request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            TotalPrice = totalPrice,
            Note = request.Note?.Trim(),
            StockItemId = request.StockItemId
        };

        serviceRecord.Operations.Add(operation);

        serviceRecord.TotalAmount = serviceRecord.Operations.Sum(x => x.TotalPrice);
        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        await _context.SaveChangesAsync();

if (operation.StockItemId.HasValue && operation.Type == OperationType.Part)
{
    var stockResult = await _stockItemService.UseForServiceOperationAsync(
        operation.StockItemId.Value,
        operation.Quantity,
        operation.UnitPrice,
        operation.Id,
        workshopId);

    if (!stockResult.IsSuccess)
    {
        await transaction.RollbackAsync();
        return ServiceResult<ServiceOperationDto>.Fail(stockResult.ErrorMessage);
    }

    await _context.SaveChangesAsync();
}

        await _auditLogService.AddAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            ActionType = AuditActionType.Add,
            EntityType = AuditEntityType.ServiceOperation,
            EntityId = operation.Id,
            Description = $"Servis operasyonu eklendi: {GetServiceRecordDisplayName(serviceRecord)} - {operation.Description}",
            NewValues = new
            {
                ServiceRecordId = serviceRecord.Id,
                serviceRecord.RecordNumber,
                operation.Type,
                operation.Description,
                operation.Quantity,
                operation.UnitPrice,
                operation.TotalPrice,
                operation.StockItemId,
                operation.ServiceRequestItemId
            }
        });

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return ServiceResult<ServiceOperationDto>.Success(new ServiceOperationDto
        {
            Id = operation.Id,
            Type = operation.Type,
            Description = operation.Description,
            Quantity = operation.Quantity,
            UnitPrice = operation.UnitPrice,
            TotalPrice = operation.TotalPrice,
            Note = operation.Note,
            ServiceRequestItemId = operation.ServiceRequestItemId,
            StockItemId = operation.StockItemId
        });
    }
    public async Task<ServiceResult<bool>> CompleteAsync(int serviceRecordId, int workshopId)
    {
        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

        if (serviceRecord.Status == ServiceRecordStatus.Completed)
            return ServiceResult<bool>.Fail("Bu servis kaydı zaten tamamlanmış.");

        var oldStatus = serviceRecord.Status;
        serviceRecord.Status = ServiceRecordStatus.Completed;
        serviceRecord.CompletedAt = _dateTimeProvider.Now;
        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        await _auditLogService.AddAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            ActionType = AuditActionType.Complete,
            EntityType = AuditEntityType.ServiceRecord,
            EntityId = serviceRecord.Id,
            Description = $"Servis kaydı tamamlandı: {GetServiceRecordDisplayName(serviceRecord)}",
            OldValues = new
            {
                Status = oldStatus
            },
            NewValues = new
            {
                serviceRecord.Status,
                serviceRecord.CompletedAt
            }
        });

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> UpdateStatusAsync(int serviceRecordId, UpdateServiceRecordStatusRequest request, int workshopId)
    {
        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

        if (!Enum.IsDefined(typeof(ServiceRecordStatus), request.Status))
            return ServiceResult<bool>.Fail("Geçersiz servis durumu.");

        var newStatus = (ServiceRecordStatus)request.Status;

        var oldStatus = serviceRecord.Status;

        serviceRecord.Status = newStatus;

        if (newStatus == ServiceRecordStatus.Completed)
            serviceRecord.CompletedAt = _dateTimeProvider.Now;
        else
            serviceRecord.CompletedAt = null;

        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        if (oldStatus != newStatus)
        {
            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = GetStatusAuditActionType(oldStatus, newStatus),
                EntityType = AuditEntityType.ServiceRecord,
                EntityId = serviceRecord.Id,
                Description = GetStatusAuditDescription(serviceRecord, oldStatus, newStatus),
                OldValues = new
                {
                    Status = oldStatus
                },
                NewValues = new
                {
                    Status = newStatus,
                    serviceRecord.CompletedAt
                }
            });
        }

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<DeleteServiceOperationResponse>> DeleteOperationAsync(int operationId, int workshopId)
    {
        var operation = await _context.ServiceOperations
            .Include(x => x.ServiceRecord)
            .FirstOrDefaultAsync(x =>
                x.Id == operationId &&
                x.ServiceRecord.WorkshopId == workshopId);

        if (operation is null)
            return ServiceResult<DeleteServiceOperationResponse>.Fail("İşlem bulunamadı.");

        var serviceRecordId = operation.ServiceRecordId;
        var serviceRequestItemId = operation.ServiceRequestItemId;

        var deletedOperationSnapshot = new
        {
            operation.Id,
            operation.ServiceRecordId,
            operation.Type,
            operation.Description,
            operation.Quantity,
            operation.UnitPrice,
            operation.TotalPrice,
            operation.StockItemId,
            operation.ServiceRequestItemId
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (operation.StockItemId.HasValue && operation.Type == OperationType.Part)
        {
            var stockResult = await _stockItemService.ReturnForServiceOperationAsync(
                operation.StockItemId.Value,
                operation.Quantity,
                operation.UnitPrice,
                operation.Id,
                workshopId);

            if (!stockResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return ServiceResult<DeleteServiceOperationResponse>.Fail(stockResult.ErrorMessage);
            }
        }

        _context.ServiceOperations.Remove(operation);

        await _context.SaveChangesAsync();

        var recordTotal = await _context.ServiceOperations
            .Where(x => x.ServiceRecordId == serviceRecordId)
            .SumAsync(x => x.TotalPrice);

        var requestItemTotal = serviceRequestItemId.HasValue
            ? await _context.ServiceOperations
                .Where(x =>
                    x.ServiceRecordId == serviceRecordId &&
                    x.ServiceRequestItemId == serviceRequestItemId.Value)
                .SumAsync(x => x.TotalPrice)
            : 0;

        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
        {
            await transaction.RollbackAsync();
            return ServiceResult<DeleteServiceOperationResponse>.Fail("Servis kaydı bulunamadı.");
        }

        serviceRecord.TotalAmount = recordTotal;
        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        await _auditLogService.AddAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            ActionType = AuditActionType.Remove,
            EntityType = AuditEntityType.ServiceOperation,
            EntityId = operationId,
            Description = $"Servis operasyonu silindi: {GetServiceRecordDisplayName(serviceRecord)} - {deletedOperationSnapshot.Description}",
            OldValues = deletedOperationSnapshot,
            NewValues = new
            {
                ServiceRecordId = serviceRecord.Id,
                RecordTotal = recordTotal,
                RequestItemTotal = requestItemTotal
            }
        });

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return ServiceResult<DeleteServiceOperationResponse>.Success(
            new DeleteServiceOperationResponse
            {
                ServiceRecordId = serviceRecordId,
                ServiceRequestItemId = serviceRequestItemId,
                RequestItemTotal = requestItemTotal,
                RecordTotal = recordTotal
            });
    }

    public async Task<ServiceResult<DeleteServiceRequestItemResponse>> DeleteRequestItemAsync(int requestItemId, int workshopId)
    {
        var requestItem = await _context.ServiceRequestItems
            .Include(x => x.ServiceRecord)
            .FirstOrDefaultAsync(x =>
                x.Id == requestItemId &&
                x.ServiceRecord.WorkshopId == workshopId);

        if (requestItem is null)
            return ServiceResult<DeleteServiceRequestItemResponse>.Fail("Talep bulunamadı.");

        var serviceRecordId = requestItem.ServiceRecordId;

        var relatedOperations = await _context.ServiceOperations
            .Where(x =>
                x.ServiceRecordId == serviceRecordId &&
                x.ServiceRequestItemId == requestItemId)
            .ToListAsync();

        _context.ServiceOperations.RemoveRange(relatedOperations);
        _context.ServiceRequestItems.Remove(requestItem);

        await _context.SaveChangesAsync();

        var recordTotal = await _context.ServiceOperations
            .Where(x => x.ServiceRecordId == serviceRecordId)
            .SumAsync(x => x.TotalPrice);

        var serviceRecord = await _context.ServiceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
                x.WorkshopId == workshopId);

        if (serviceRecord is null)
            return ServiceResult<DeleteServiceRequestItemResponse>.Fail("Servis kaydı bulunamadı.");

        serviceRecord.TotalAmount = recordTotal;

        serviceRecord.UpdatedAt = _dateTimeProvider.Now;

        await _context.SaveChangesAsync();

        return ServiceResult<DeleteServiceRequestItemResponse>.Success(
            new DeleteServiceRequestItemResponse
            {
                ServiceRecordId = serviceRecordId,
                ServiceRequestItemId = requestItemId,
                RecordTotal = recordTotal
            });
    }

    public async Task<ServiceResult<PagedResult<ServiceRecordListItemDto>>> GetPagedAsync(int workshopId, ServiceRecordListQueryDto query)
    {
        query.Search = string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim();

        query.PageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        query.PageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        query.PageSize = query.PageSize > 100 ? 100 : query.PageSize;

        var recordsQuery = _context.ServiceRecords
            .AsNoTracking()
            .Where(x => x.WorkshopId == workshopId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search;
            var normalizedPlateSearch = search.Replace(" ", "").ToUpperInvariant();

            recordsQuery = recordsQuery.Where(x =>
                x.RecordNumber.Contains(search) ||
                x.CustomerNameSnapshot.Contains(search) ||
                x.CustomerPhoneSnapshot.Contains(search) ||
                x.VehiclePlateSnapshot.Contains(normalizedPlateSearch) ||
                (x.VehicleBrandNameSnapshot != null && x.VehicleBrandNameSnapshot.Contains(search)) ||
                (x.VehicleModelNameSnapshot != null && x.VehicleModelNameSnapshot.Contains(search)));
        }

        var statusFilter = string.IsNullOrWhiteSpace(query.StatusFilter)
    ? "active"
    : query.StatusFilter.Trim().ToLowerInvariant();

        recordsQuery = statusFilter switch
        {
            "active" => recordsQuery.Where(x =>
                x.Status == ServiceRecordStatus.Open ||
                x.Status == ServiceRecordStatus.InProgress),

            "completed" => recordsQuery.Where(x =>
                x.Status == ServiceRecordStatus.Completed),

            "cancelled" => recordsQuery.Where(x =>
                x.Status == ServiceRecordStatus.Cancelled),

            "all" => recordsQuery,

            _ => recordsQuery.Where(x =>
                x.Status == ServiceRecordStatus.Open ||
                x.Status == ServiceRecordStatus.InProgress)
        };

        if (query.CreatedFrom.HasValue)
        {
            recordsQuery = recordsQuery.Where(x => x.CreatedAt >= query.CreatedFrom.Value.Date);
        }

        if (query.CreatedTo.HasValue)
        {
            var createdToExclusive = query.CreatedTo.Value.Date.AddDays(1);
            recordsQuery = recordsQuery.Where(x => x.CreatedAt < createdToExclusive);
        }

        var totalCount = await recordsQuery.CountAsync();

        var items = await recordsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
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

        var pagedResult = new PagedResult<ServiceRecordListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return ServiceResult<PagedResult<ServiceRecordListItemDto>>.Success(pagedResult);
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

    private string GenerateRecordNumber()
    {
        return $"SR-{_dateTimeProvider.Now:yyyyMMddHHmmssfff}";
    }

    private static string GetServiceRecordDisplayName(ServiceRecord serviceRecord)
    {
        return $"{serviceRecord.RecordNumber} / {serviceRecord.VehiclePlateSnapshot}";
    }

    private static AuditActionType GetStatusAuditActionType(
        ServiceRecordStatus oldStatus,
        ServiceRecordStatus newStatus)
    {
        if (newStatus == ServiceRecordStatus.Completed)
            return AuditActionType.Complete;

        if (newStatus == ServiceRecordStatus.Cancelled)
            return AuditActionType.Cancel;

        if (oldStatus == ServiceRecordStatus.Cancelled &&
            newStatus != ServiceRecordStatus.Cancelled)
            return AuditActionType.Reopen;

        return AuditActionType.Update;
    }

    private static string GetStatusAuditDescription(
        ServiceRecord serviceRecord,
        ServiceRecordStatus oldStatus,
        ServiceRecordStatus newStatus)
    {
        var displayName = GetServiceRecordDisplayName(serviceRecord);

        if (newStatus == ServiceRecordStatus.Completed)
            return $"Servis kaydı tamamlandı: {displayName}";

        if (newStatus == ServiceRecordStatus.Cancelled)
            return $"Servis kaydı iptal edildi: {displayName}";

        if (oldStatus == ServiceRecordStatus.Cancelled &&
            newStatus != ServiceRecordStatus.Cancelled)
            return $"Servis kaydı tekrar aktif yapıldı: {displayName}";

        return $"Servis kaydı durumu güncellendi: {displayName} ({oldStatus} → {newStatus})";
    }


}