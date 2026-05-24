using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Onboarding;
using Payroll.Application.Queries.Onboarding;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/onboarding")]
[Authorize]
public sealed class OnboardingController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var dto = await sender.Send(new GetOnboardingStatusQuery(), ct);
        return Ok(dto);
    }

    [HttpPost("seed-defaults/{step}")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> SeedDefaults(string step, CancellationToken ct)
    {
        try
        {
            await sender.Send(new SeedOnboardingDefaultsCommand(step, GetActorId()), ct);
        }
        catch (DomainException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
        var status = await sender.Send(new GetOnboardingStatusQuery(), ct);
        return Ok(status);
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}
