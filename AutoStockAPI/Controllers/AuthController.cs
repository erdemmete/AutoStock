using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Auth;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var token = await _authService.LoginAsync(request);
        return Ok(token);
    }

    [AllowAnonymous]
    [HttpPost("password-setup/validate")]
    public async Task<IActionResult> ValidatePasswordSetupToken(
    ValidatePasswordActionTokenRequestDto request)
    {
        var result = await _authService.ValidatePasswordSetupTokenAsync(request);

        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("password-setup/complete")]
    public async Task<IActionResult> CompletePasswordSetup(
        CompletePasswordActionRequestDto request)
    {
        var result = await _authService.CompletePasswordSetupAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset/validate")]
    public async Task<IActionResult> ValidatePasswordResetToken(
        ValidatePasswordActionTokenRequestDto request)
    {
        var result = await _authService.ValidatePasswordResetTokenAsync(request);

        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset/complete")]
    public async Task<IActionResult> CompletePasswordReset(
        CompletePasswordActionRequestDto request)
    {
        var result = await _authService.CompletePasswordResetAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("password-setup/code/validate")]
    public async Task<IActionResult> ValidatePasswordSetupCode(
    ValidatePasswordActionCodeRequestDto request)
    {
        var result = await _authService.ValidatePasswordSetupCodeAsync(request);

        return StatusCode(result.StatusCode, result);
    }

    [AllowAnonymous]
    [HttpPost("password-setup/code/complete")]
    public async Task<IActionResult> CompletePasswordSetupByCode(
        CompletePasswordActionCodeRequestDto request)
    {
        var result = await _authService.CompletePasswordSetupByCodeAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return StatusCode(result.StatusCode, result);
    }
}