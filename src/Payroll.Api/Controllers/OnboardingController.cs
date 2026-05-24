using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Queries.Onboarding;

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
}
