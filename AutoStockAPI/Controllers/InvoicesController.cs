using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : BaseApiController
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet("draft/from-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetDraftFromServiceRecord(int serviceRecordId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetCreateDraftAsync(
                serviceRecordId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("from-service-record/{serviceRecordId:int}/draft")]
        public async Task<IActionResult> CreateOrGetDraftFromServiceRecord(int serviceRecordId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.CreateOrGetDraftFromServiceRecordAsync(
                serviceRecordId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInvoiceDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.CreateAsync(
                request,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("{invoiceId:int}")]
        public async Task<IActionResult> GetDetail(int invoiceId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetDetailAsync(
                invoiceId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{invoiceId:int}/issue")]
        public async Task<IActionResult> Issue(int invoiceId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.IssueAsync(
                invoiceId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] InvoiceListQueryDto query)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetPagedAsync(
                query,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetListByServiceRecord(int serviceRecordId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetListByServiceRecordAsync(
                serviceRecordId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("draft/by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetDraftByServiceRecord(int serviceRecordId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetDraftByServiceRecordAsync(
                serviceRecordId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("active/by-service-record/{serviceRecordId:int}")]
        public async Task<IActionResult> GetActiveInvoiceByServiceRecord(int serviceRecordId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.GetActiveInvoiceByServiceRecordAsync(
                serviceRecordId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{invoiceId:int}/cancel")]
        public async Task<IActionResult> Cancel(int invoiceId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _invoiceService.CancelAsync(
                invoiceId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPut("{invoiceId:int}")]
        public async Task<IActionResult> Update(int invoiceId, UpdateInvoiceDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            request.InvoiceId = invoiceId;

            var result = await _invoiceService.UpdateAsync(
                request,
                workshopIdResult.Data);

            return ToActionResult(result);
        }
    }
}