using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
        {
            return Unauthorized();
        }

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.SearchAsync(query, workshopId);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
            return Unauthorized();

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.GetListAsync(workshopId);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto request)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
            return Unauthorized();

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.CreateAsync(request, workshopId);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
            return Unauthorized();

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.GetByIdAsync(id, workshopId);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerDto request)
    {
        if (id != request.Id)
            return BadRequest(ServiceResult<int>.Fail("Müşteri bilgisi hatalı."));

        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
            return Unauthorized();

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.UpdateAsync(request, workshopId);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/passive")]
    public async Task<IActionResult> SetPassive(int id)
    {
        var workshopIdClaim = User.FindFirst("workshopId")?.Value;

        if (string.IsNullOrWhiteSpace(workshopIdClaim))
            return Unauthorized();

        var workshopId = int.Parse(workshopIdClaim);

        var result = await _customerService.SetPassiveAsync(id, workshopId);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}