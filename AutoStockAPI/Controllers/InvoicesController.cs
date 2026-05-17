
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("draft/from-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetDraftFromServiceRecord(int serviceRecordId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("WorkshopId bilgisi token içinde bulunamadı.");

            var result = await _invoiceService.GetCreateDraftAsync(serviceRecordId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInvoiceDto request)
        {
            var workshopIdClaim = User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.CreateAsync(request, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}