using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Payroll.Application.Commands.Auth;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword(
        [FromBody] SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new SetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);
            return Ok(new { message = "Password set successfully. You can now log in." });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        }
        catch
        {
            // Swallow all errors — no user enumeration
        }
        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }
}

public record SetPasswordRequest(string Email, string Token, string NewPassword);
public record ForgotPasswordRequest(string Email);