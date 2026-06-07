using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceRecordsController : ControllerBase
{
    private readonly IServiceRecordService _serviceRecordService;

    public ServiceRecordsController(IServiceRecordService serviceRecordService)
    {
        _serviceRecordService = serviceRecordService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRecordRequest request)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.CreateAsync(request, workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ServiceRecordListQueryDto query)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.GetPagedAsync(workshopId, query);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.GetDetailAsync(id, workshopId);

        if (result.IsFailure)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPut("request-items/{requestItemId:int}")]
    public async Task<IActionResult> UpdateRequestItem(
        int requestItemId,
        UpdateServiceRequestItemRequest request)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.UpdateRequestItemAsync(
            requestItemId,
            request,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{serviceRecordId:int}/request-items")]
    public async Task<IActionResult> AddRequestItem(
        int serviceRecordId,
        CreateServiceRequestItemDto request)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.AddRequestItemAsync(
            serviceRecordId,
            request,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{serviceRecordId:int}/operations")]
    public async Task<IActionResult> AddOperation(
        int serviceRecordId,
        AddServiceOperationRequest request)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.AddOperationAsync(
            serviceRecordId,
            request,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.CompleteAsync(id, workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateServiceRecordStatusRequest request)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.UpdateStatusAsync(
            id,
            request,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("operations/{operationId:int}")]
    public async Task<IActionResult> DeleteOperation(int operationId)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.DeleteOperationAsync(
            operationId,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("request-items/{requestItemId:int}")]
    public async Task<IActionResult> DeleteRequestItem(int requestItemId)
    {
        if (!TryGetWorkshopId(out var workshopId))
            return Unauthorized(ServiceResult<object>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.DeleteRequestItemAsync(
            requestItemId,
            workshopId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    private bool TryGetWorkshopId(out int workshopId)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value
            ?? User.FindFirst("WorkshopId")?.Value;

        return int.TryParse(workshopIdClaim, out workshopId);
    }
}