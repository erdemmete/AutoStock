using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.StockItems;
using Services.DTOs.StockItems;

namespace Services.Interfaces.StockItems
{
    public interface IStockItemService
    {
        Task<List<StockItemListDto>> GetAllAsync(int workshopId);
        Task<StockItemDetailDto?> GetByIdAsync(int id, int workshopId);
        Task<int> CreateAsync(CreateStockItemDto dto, int workshopId);
        Task<ServiceResult<int>> UpdateAsync(UpdateStockItemDto dto, int workshopId);
        Task<ServiceResult<int>> SetPassiveAsync(int id, int workshopId);
        Task<ServiceResult<int>> AdjustStockAsync(int stockItemId, AdjustStockDto dto, int workshopId);
        Task<List<StockMovementListDto>> GetMovementsAsync(int stockItemId, int workshopId);
        Task<ServiceResult<int>> StockInAsync(int stockItemId, StockTransactionDto dto, int workshopId);

        Task<ServiceResult<int>> StockOutAsync(int stockItemId, StockTransactionDto dto, int workshopId);
        Task<ServiceResult<int>> UseForInvoiceAsync(int stockItemId, decimal quantity, decimal? unitPrice, int invoiceId, int workshopId);
        Task<List<StockItemSelectDto>> GetSelectListAsync(int workshopId);
        Task<List<StockItemSelectDto>> SearchAsync(int workshopId, string? query);
    }
}