using AutoStock.Services.Interfaces;
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
}