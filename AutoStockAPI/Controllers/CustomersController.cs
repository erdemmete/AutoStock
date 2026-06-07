using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.SearchAsync(query, workshopIdResult.Data);

        return Ok(ServiceResult<List<CustomerSearchDto>>.Success(result));
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] CustomerListQueryDto query)
    {
        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.GetPagedAsync(query, workshopIdResult.Data);

        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto request)
    {
        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.CreateAsync(request, workshopIdResult.Data);

        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.GetByIdAsync(id, workshopIdResult.Data);

        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerDto request)
    {
        if (id != request.Id)
        {
            return BadRequest(
                ServiceResult<int>.Fail(
                    "Müşteri bilgisi hatalı.",
                    HttpStatusCode.BadRequest));
        }

        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.UpdateAsync(request, workshopIdResult.Data);

        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    [HttpPost("{id:int}/passive")]
    public async Task<IActionResult> SetPassive(int id)
    {
        var workshopIdResult = GetWorkshopId();

        if (workshopIdResult.IsFailure)
            return Unauthorized(workshopIdResult);

        var result = await _customerService.SetPassiveAsync(id, workshopIdResult.Data);

        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, result);

        return Ok(result);
    }

    private ServiceResult<int> GetWorkshopId()
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value
            ?? User.FindFirst("WorkshopId")?.Value;

        if (!int.TryParse(workshopIdClaim, out var workshopId))
        {
            return ServiceResult<int>.Fail(
                "Workshop bilgisi bulunamadı.",
                HttpStatusCode.Unauthorized);
        }

        return ServiceResult<int>.Success(workshopId);
    }
}