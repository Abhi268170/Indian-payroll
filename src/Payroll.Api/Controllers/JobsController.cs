using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Domain.Interfaces;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public sealed class JobsController(IJobProgressService jobProgress, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{jobId}/status")]
    public async Task<IActionResult> GetStatus(string jobId, CancellationToken ct)
    {
        JobProgressDto? dto = await jobProgress.GetAsync(tenantContext.TenantId, jobId, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }
}
