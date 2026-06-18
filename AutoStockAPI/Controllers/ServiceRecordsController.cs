using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
public class ServiceRecordsController : BaseApiController
{
    private readonly IServiceRecordService _serviceRecordService;
    private readonly IVehicleService _vehicleService;

    public ServiceRecordsController(IServiceRecordService serviceRecordService, IVehicleService vehicleService)
    {
        _serviceRecordService = serviceRecordService;
        _vehicleService = vehicleService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRecordRequest request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.CreateAsync(
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ServiceRecordListQueryDto query)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.GetPagedAsync(
            workshopIdResult.Data,
            query);

        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.GetDetailAsync(
            id,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("request-items/{requestItemId:int}")]
    public async Task<IActionResult> UpdateRequestItem(
        int requestItemId,
        UpdateServiceRequestItemRequest request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.UpdateRequestItemAsync(
            requestItemId,
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPost("{serviceRecordId:int}/request-items")]
    public async Task<IActionResult> AddRequestItem(
        int serviceRecordId,
        CreateServiceRequestItemDto request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.AddRequestItemAsync(
            serviceRecordId,
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPost("{serviceRecordId:int}/operations")]
    public async Task<IActionResult> AddOperation(
        int serviceRecordId,
        AddServiceOperationRequest request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.AddOperationAsync(
            serviceRecordId,
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("operations/{operationId:int}")]
    public async Task<IActionResult> UpdateOperation(
    int operationId,
    UpdateServiceOperationRequest request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.UpdateOperationAsync(
            operationId,
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.CompleteAsync(
            id,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateServiceRecordStatusRequest request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.UpdateStatusAsync(
            id,
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet("search-vehicles")]
    public async Task<IActionResult> SearchVehicles([FromQuery] string plate)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _vehicleService.SearchByPlateAsync(
            plate,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet("vehicles/{vehicleId:int}/prefill")]
    public async Task<IActionResult> GetVehiclePrefill(int vehicleId)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _vehicleService.GetByIdAsync(
            vehicleId,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpDelete("operations/{operationId:int}")]
    public async Task<IActionResult> DeleteOperation(int operationId)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.DeleteOperationAsync(
            operationId,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpDelete("request-items/{requestItemId:int}")]
    public async Task<IActionResult> DeleteRequestItem(int requestItemId)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.DeleteRequestItemAsync(
            requestItemId,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("request-items/{requestItemId:int}/restore")]
    public async Task<IActionResult> RestoreRequestItem(int requestItemId)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _serviceRecordService.RestoreRequestItemAsync(
            requestItemId,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet("create-workshop-info")]
    public async Task<IActionResult> GetCreateWorkshopInfo()
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
            return Unauthorized(ServiceResult<ServiceRecordCreateWorkshopInfoDto>.Fail("Workshop bilgisi bulunamadı."));

        var result = await _serviceRecordService.GetCreateWorkshopInfoAsync(workshopId);

        return ToActionResult(result);
    }
}
