using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/payroll-runs")]
[Authorize]
public sealed class PayrollRunsController(ISender sender) : ControllerBase
{
    [HttpGet("current-period")]
    public async Task<IActionResult> GetCurrentPeriod(CancellationToken ct)
    {
        CurrentPayPeriodDto? dto = await sender.Send(new GetCurrentPayPeriodQuery(), ct);
        if (dto is null) return NoContent();
        return Ok(dto);
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(CancellationToken ct)
    {
        try
        {
            Guid actorId = GetActorId();
            PayrollRunSummaryDto dto = await sender.Send(new InitiatePayrollRunCommand(actorId), ct);
            return CreatedAtAction(nameof(GetSummary), new { id = dto.Id }, dto);
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct)
    {
        try
        {
            PayrollRunSummaryDto dto = await sender.Send(new GetPayrollRunSummaryQuery(id), ct);
            return Ok(dto);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}
