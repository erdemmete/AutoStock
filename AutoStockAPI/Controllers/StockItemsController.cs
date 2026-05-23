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
        public async Task<IActionResult> GetAll()
        {
            var workshopId = GetWorkshopId();

            var result = await _stockItemService.GetAllAsync(workshopId.Value);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var workshopId = GetWorkshopId();

            var result = await _stockItemService.GetByIdAsync(id, workshopId.Value);

            if (result == null)
                return NotFound("Stok kartı bulunamadı.");

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStockItemDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized();

            var stockItemId = await _stockItemService.CreateAsync(dto, workshopId.Value);

            return Ok(new
            {
                id = stockItemId,
                message = "Stok kartı başarıyla oluşturuldu."
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateStockItemDto dto)
        {
            if (id != dto.Id)
                return BadRequest(ServiceResult<int>.Fail("Stok bilgisi hatalı."));

            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized();

            var result = await _stockItemService.UpdateAsync(dto, workshopId.Value);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{id:int}/passive")]
        public async Task<IActionResult> SetPassive(int id)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized();

            var result = await _stockItemService.SetPassiveAsync(id, workshopId.Value);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{id:int}/adjust-stock")]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockDto dto)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized();

            var result = await _stockItemService.AdjustStockAsync(id, dto, workshopId.Value);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{id:int}/movements")]
        public async Task<IActionResult> GetMovements(int id)
        {
            var workshopId = GetWorkshopId();

            if (workshopId == null)
                return Unauthorized();

            var result = await _stockItemService.GetMovementsAsync(id, workshopId.Value);

            return Ok(result);
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