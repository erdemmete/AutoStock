using AutoStock.API.Controllers;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.OwnerOrStaff)]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var customers = await _customerService.SearchAsync(query, workshopIdResult.Data);

        var result = ServiceResult<List<CustomerSearchDto>>.Success(customers);

        return ToActionResult(result);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<IActionResult> GetList([FromQuery] CustomerListQueryDto query)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _customerService.GetPagedAsync(query, workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<IActionResult> Create(CreateCustomerDto request)
    {
        return await CreateCustomerAsync(request);
    }

    [HttpPost("quick-create")]
    public async Task<IActionResult> QuickCreate(CreateCustomerDto request)
    {
        return await CreateCustomerAsync(request);
    }

    private async Task<IActionResult> CreateCustomerAsync(CreateCustomerDto request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _customerService.CreateAsync(request, workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<IActionResult> GetById(int id)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _customerService.GetByIdAsync(id, workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<IActionResult> Update(int id, UpdateCustomerDto request)
    {
        if (id != request.Id)
        {
            var badRequestResult = ServiceResult<int>.Fail(
                "Müşteri bilgisi hatalı.",
                HttpStatusCode.BadRequest);

            return ToActionResult(badRequestResult);
        }

        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _customerService.UpdateAsync(request, workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpPost("{id:int}/passive")]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<IActionResult> SetPassive(int id)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _customerService.SetPassiveAsync(id, workshopIdResult.Data);

        return ToActionResult(result);
    }
}
