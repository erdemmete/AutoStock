using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.StockItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs.StockItems;
using Services.Interfaces.StockItems;
using System.Net;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.OwnerOrStaff)]
    [ApiController]
    [Route("api/[controller]")]
    public class StockItemsController : BaseApiController
    {
        private readonly IStockItemService _stockItemService;

        public StockItemsController(IStockItemService stockItemService)
        {
            _stockItemService = stockItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] StockItemListQueryDto query)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.GetPagedAsync(
                workshopIdResult.Data,
                query);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.GetByIdAsync(
                id,
                workshopIdResult.Data);

            if (result == null)
            {
                return ToActionResult(ServiceResult<object>.Fail(
                    "Stok kartı bulunamadı.",
                    HttpStatusCode.NotFound));
            }

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStockItemDto dto)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var stockItemId = await _stockItemService.CreateAsync(
                dto,
                workshopIdResult.Data);

            return Ok(ServiceResult<object>.Success(new
            {
                id = stockItemId,
                message = "Stok kartı başarıyla oluşturuldu."
            }));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.Owner)]
        public async Task<IActionResult> Update(int id, UpdateStockItemDto dto)
        {
            if (id != dto.Id)
            {
                return ToActionResult(ServiceResult<int>.Fail(
                    "Stok bilgisi hatalı.",
                    HttpStatusCode.BadRequest));
            }

            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.UpdateAsync(
                dto,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{id:int}/passive")]
        [Authorize(Roles = AppRoles.Owner)]
        public async Task<IActionResult> SetPassive(int id)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.SetPassiveAsync(
                id,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{id:int}/adjust-stock")]
        [Authorize(Roles = AppRoles.Owner)]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockDto dto)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.AdjustStockAsync(
                id,
                dto,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("{id:int}/movements")]
        public async Task<IActionResult> GetMovements(int id)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.GetMovementsAsync(
                id,
                workshopIdResult.Data);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpPost("{id:int}/stock-in")]
        public async Task<IActionResult> StockIn(int id, StockTransactionDto dto)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.StockInAsync(
                id,
                dto,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{id:int}/stock-out")]
        [Authorize(Roles = AppRoles.Owner)]
        public async Task<IActionResult> StockOut(int id, StockTransactionDto dto)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.StockOutAsync(
                id,
                dto,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("select-list")]
        public async Task<IActionResult> GetSelectList()
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.GetSelectListAsync(
                workshopIdResult.Data);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.SearchAsync(
                workshopIdResult.Data,
                q);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _stockItemService.GetFilterOptionsAsync(
                workshopIdResult.Data);

            return Ok(ServiceResult<object>.Success(result));
        }
    }
}
