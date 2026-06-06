using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.StockItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.DTOs.StockItems;
using Services.Interfaces.StockItems;

namespace AutoStockAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StockItemsController : ControllerBase
    {
        private readonly IStockItemService _stockItemService;

        public StockItemsController(IStockItemService stockItemService)
        {
            _stockItemService = stockItemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] StockItemListQueryDto query)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.GetPagedAsync(
                workshopId.Value,
                query);

            return Ok(ServiceResult<object>.Success(result));
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.GetByIdAsync(id, workshopId.Value);

            if (result == null)
                return NotFound(ServiceResult<object>.Fail("Stok kartı bulunamadı."));

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStockItemDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var stockItemId = await _stockItemService.CreateAsync(dto, workshopId.Value);

            return Ok(ServiceResult<object>.Success(new
            {
                id = stockItemId,
                message = "Stok kartı başarıyla oluşturuldu."
            }));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateStockItemDto dto)
        {
            if (id != dto.Id)
                return BadRequest(ServiceResult<int>.Fail("Stok bilgisi hatalı."));

            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.UpdateAsync(dto, workshopId.Value);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost("{id:int}/passive")]
        public async Task<IActionResult> SetPassive(int id)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.SetPassiveAsync(id, workshopId.Value);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost("{id:int}/adjust-stock")]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.AdjustStockAsync(id, dto, workshopId.Value);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet("{id:int}/movements")]
        public async Task<IActionResult> GetMovements(int id)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.GetMovementsAsync(id, workshopId.Value);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpPost("{id:int}/stock-in")]
        public async Task<IActionResult> StockIn(int id, StockTransactionDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.StockInAsync(id, dto, workshopId.Value);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpPost("{id:int}/stock-out")]
        public async Task<IActionResult> StockOut(int id, StockTransactionDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.StockOutAsync(id, dto, workshopId.Value);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result);

            return Ok(result);
        }

        [HttpGet("select-list")]
        public async Task<IActionResult> GetSelectList()
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.GetSelectListAsync(workshopId.Value);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.SearchAsync(workshopId.Value, q);

            return Ok(ServiceResult<object>.Success(result));
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized(ServiceResult<object>.Fail("Oturum bilgisi bulunamadı."));

            var result = await _stockItemService.GetFilterOptionsAsync(workshopId.Value);

            return Ok(ServiceResult<object>.Success(result));
        }
        private int? GetWorkshopId()
        {
            var workshopIdClaim = User.FindFirst("workshopId")?.Value;

            if (string.IsNullOrWhiteSpace(workshopIdClaim))
                return null;

            return int.Parse(workshopIdClaim);
        }


    }
}