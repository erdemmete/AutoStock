using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
            return Unauthorized("Workshop bilgisi bulunamadı.");

        var result = await _serviceRecordService.CreateAsync(request, workshopId);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
            return Unauthorized("Workshop bilgisi bulunamadı.");

        var result = await _serviceRecordService.GetListAsync(workshopId);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
            return Unauthorized("Workshop bilgisi bulunamadı.");

        var result = await _serviceRecordService.GetDetailAsync(id, workshopId);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPut("request-items/{requestItemId:int}")]
    public async Task<IActionResult> UpdateRequestItem(
    int requestItemId,
    UpdateServiceRequestItemRequest request)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
            return Unauthorized("Workshop bilgisi bulunamadı.");

        var result = await _serviceRecordService.UpdateRequestItemAsync(
            requestItemId,
            request,
            workshopId);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}