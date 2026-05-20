
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

        [HttpGet("{invoiceId:int}")]
        public async Task<IActionResult> GetDetail(int invoiceId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.GetDetailAsync(invoiceId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("{invoiceId:int}/issue")]
        public async Task<IActionResult> Issue(int invoiceId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.IssueAsync(invoiceId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.GetListAsync(workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetListByServiceRecord(int serviceRecordId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.GetListByServiceRecordAsync(serviceRecordId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("draft/by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetDraftByServiceRecord(int serviceRecordId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.GetDraftByServiceRecordAsync(serviceRecordId, workshopId);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("active/by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetActiveInvoiceByServiceRecord(int serviceRecordId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.GetActiveInvoiceByServiceRecordAsync(serviceRecordId, workshopId);

            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPost("{invoiceId:int}/cancel")]
        public async Task<IActionResult> Cancel(int invoiceId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _invoiceService.CancelAsync(invoiceId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{invoiceId:int}")]
        public async Task<IActionResult> Update(int invoiceId, UpdateInvoiceDto request)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            request.InvoiceId = invoiceId;

            var result = await _invoiceService.UpdateAsync(request, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
    }
}