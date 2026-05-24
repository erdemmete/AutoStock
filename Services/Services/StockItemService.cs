using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.StockItems;
using Microsoft.EntityFrameworkCore;
using Services.DTOs.StockItems;
using Services.Interfaces.StockItems;

namespace Services.Services.StockItems
{
    public class StockItemService : IStockItemService
    {
        private readonly AppDbContext _context;

        public StockItemService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockItemListDto>> GetAllAsync(int workshopId)
        {
            return await _context.StockItems
                .Where(x => x.WorkshopId == workshopId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new StockItemListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Barcode = x.Barcode,
                    Brand = x.Brand,
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    SalePrice = x.SalePrice,
                    MinimumQuantity = x.MinimumQuantity
                })
                .ToListAsync();
        }

        public async Task<StockItemDetailDto?> GetByIdAsync(int id, int workshopId)
        {
            return await _context.StockItems
                .Where(x => x.Id == id && x.WorkshopId == workshopId && x.IsActive)
                .Select(x => new StockItemDetailDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Barcode = x.Barcode,
                    Brand = x.Brand,
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    PurchasePrice = x.PurchasePrice,
                    SalePrice = x.SalePrice,
                    MinimumQuantity = x.MinimumQuantity,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(CreateStockItemDto dto, int workshopId)
        {
            var stockItem = new StockItem
            {
                WorkshopId = workshopId,
                Name = dto.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim(),
                Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
                Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim(),
                Unit = dto.Unit.Trim(),
                Quantity = dto.Quantity,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                MinimumQuantity = dto.MinimumQuantity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockItems.Add(stockItem);

            if (dto.Quantity > 0)
            {
                var movement = new StockMovement
                {
                    WorkshopId = workshopId,
                    StockItem = stockItem,
                    MovementType = StockMovementType.In,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.PurchasePrice,
                    Description = "İlk stok girişi",
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(movement);
            }

            await _context.SaveChangesAsync();

            return stockItem.Id;
        }

        public async Task<ServiceResult<int>> UpdateAsync(UpdateStockItemDto dto, int workshopId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.WorkshopId == workshopId && x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            stockItem.Name = dto.Name.Trim();
            stockItem.Code = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim();
            stockItem.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
            stockItem.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim();
            stockItem.Unit = dto.Unit.Trim();
            stockItem.PurchasePrice = dto.PurchasePrice;
            stockItem.SalePrice = dto.SalePrice;
            stockItem.MinimumQuantity = dto.MinimumQuantity;

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<ServiceResult<int>> SetPassiveAsync(int id, int workshopId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x => x.Id == id && x.WorkshopId == workshopId && x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            stockItem.IsActive = false;

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<ServiceResult<int>> AdjustStockAsync(int stockItemId, AdjustStockDto dto, int workshopId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x => x.Id == stockItemId && x.WorkshopId == workshopId && x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            if (stockItem.Quantity == dto.NewQuantity)
                return ServiceResult<int>.Fail("Stok miktarında değişiklik yok.");

            var oldQuantity = stockItem.Quantity;
            var difference = dto.NewQuantity - oldQuantity;

            stockItem.Quantity = dto.NewQuantity;

            var movement = new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.Adjustment,
                Quantity = Math.Abs(difference),
                UnitPrice = stockItem.PurchasePrice,
                Description = string.IsNullOrWhiteSpace(dto.Description)
                    ? $"Stok sayım düzeltmesi. Eski miktar: {oldQuantity}, yeni miktar: {dto.NewQuantity}"
                    : dto.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<List<StockMovementListDto>> GetMovementsAsync(int stockItemId, int workshopId)
        {
            var stockItemExists = await _context.StockItems
                .AnyAsync(x => x.Id == stockItemId && x.WorkshopId == workshopId && x.IsActive);

            if (!stockItemExists)
                return new List<StockMovementListDto>();

            return await _context.StockMovements
                .Where(x => x.StockItemId == stockItemId && x.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new StockMovementListDto
                {
                    Id = x.Id,
                    MovementType = x.MovementType.ToString(),
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    Description = x.Description,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ServiceResult<int>> StockInAsync(int stockItemId, StockTransactionDto dto,int workshopId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            stockItem.Quantity += dto.Quantity;

            var movement = new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.In,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice ?? stockItem.PurchasePrice,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<ServiceResult<int>> StockOutAsync(int stockItemId, StockTransactionDto dto, int workshopId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            if (stockItem.Quantity < dto.Quantity)
                return ServiceResult<int>.Fail("Yetersiz stok.");

            stockItem.Quantity -= dto.Quantity;

            var movement = new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.Out,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice ?? stockItem.PurchasePrice,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<ServiceResult<int>> UseForInvoiceAsync(int stockItemId, decimal quantity, decimal? unitPrice, int invoiceId, int workshopId)
        {
            if (quantity <= 0)
                return ServiceResult<int>.Fail("Stoktan düşülecek miktar sıfırdan büyük olmalıdır.");

            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (stockItem == null)
                return ServiceResult<int>.Fail("Stok kartı bulunamadı.");

            if (stockItem.Quantity < quantity)
                return ServiceResult<int>.Fail($"{stockItem.Name} için yeterli stok yok.");

            stockItem.Quantity -= quantity;

            var movement = new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.InvoiceUsage,
                Quantity = quantity,
                UnitPrice = unitPrice ?? stockItem.SalePrice,
                Description = "Fatura kaynaklı stok çıkışı",
                ReferenceType = "Invoice",
                ReferenceId = invoiceId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            return ServiceResult<int>.Success(stockItem.Id);
        }

        public async Task<List<StockItemSelectDto>> GetSelectListAsync(int workshopId)
        {
            return await _context.StockItems
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new StockItemSelectDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Quantity = x.Quantity
                })
                .ToListAsync();
        }

        public async Task<List<StockItemSelectDto>> SearchAsync(int workshopId, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<StockItemSelectDto>();
            }

            query = query.Trim();

            return await _context.StockItems
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    (
                        x.Name.Contains(query) ||
                        (x.Code != null && x.Code.Contains(query))
                    ))
                .OrderBy(x => x.Name)
                .Take(5)
                .Select(x => new StockItemSelectDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    Quantity = x.Quantity,
                    SalePrice = x.SalePrice
                })
                .ToListAsync();
        }

        public async Task<ServiceResult<bool>> UseForServiceOperationAsync(
    int stockItemId,
    decimal quantity,
    decimal unitPrice,
    int serviceOperationId,
    int workshopId)
        {
            if (quantity <= 0)
                return ServiceResult<bool>.Fail("Stok çıkış miktarı 0'dan büyük olmalıdır.");

            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (stockItem is null)
                return ServiceResult<bool>.Fail("Stok kartı bulunamadı.");

            if (stockItem.Quantity < quantity)
                return ServiceResult<bool>.Fail("Yetersiz stok.");

            stockItem.Quantity -= quantity;

            _context.StockMovements.Add(new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.ServiceUsage,
                Quantity = quantity,
                UnitPrice = unitPrice,
                ReferenceType = "ServiceOperation",
                ReferenceId = serviceOperationId,
                Description = "Servis operasyonu kaynaklı stok çıkışı",
                CreatedAt = DateTime.UtcNow
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ReturnForServiceOperationAsync(
            int stockItemId,
            decimal quantity,
            decimal unitPrice,
            int serviceOperationId,
            int workshopId)
        {
            if (quantity <= 0)
                return ServiceResult<bool>.Fail("Stok iade miktarı 0'dan büyük olmalıdır.");

            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId);

            if (stockItem is null)
                return ServiceResult<bool>.Fail("Stok kartı bulunamadı.");

            stockItem.Quantity += quantity;

            _context.StockMovements.Add(new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.ServiceUsageReturn,
                Quantity = quantity,
                UnitPrice = unitPrice,
                ReferenceType = "ServiceOperation",
                ReferenceId = serviceOperationId,
                Description = "Servis operasyonu silme kaynaklı stok iadesi",
                CreatedAt = DateTime.UtcNow
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ReturnForInvoiceCancellationAsync(int stockItemId, decimal quantity, decimal unitPrice, int invoiceId, int workshopId)
        {
            if (quantity <= 0)
                return ServiceResult<bool>.Fail("Stok iade miktarı 0'dan büyük olmalıdır.");

            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(x =>
                    x.Id == stockItemId &&
                    x.WorkshopId == workshopId);

            if (stockItem is null)
                return ServiceResult<bool>.Fail("Stok kartı bulunamadı.");

            stockItem.Quantity += quantity;

            _context.StockMovements.Add(new StockMovement
            {
                WorkshopId = workshopId,
                StockItemId = stockItem.Id,
                MovementType = StockMovementType.InvoiceCancellation,
                Quantity = quantity,
                UnitPrice = unitPrice,
                ReferenceType = "Invoice",
                ReferenceId = invoiceId,
                Description = "Hızlı fatura iptali kaynaklı stok iadesi",
                CreatedAt = DateTime.UtcNow
            });

            return ServiceResult<bool>.Success(true);
        }
    }
}