using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
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

    [HttpGet("{id:guid}/bank-advice")]
    public async Task<IActionResult> GetBankAdvice(Guid id, CancellationToken ct)
    {
        try
        {
            BankAdviceDto data = await sender.Send(new GetBankAdviceQuery(id), ct);
            return Ok(data);
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/bank-advice/download")]
    public async Task<IActionResult> DownloadBankAdvice(Guid id, [FromServices] IBankAdviceGenerator generator, CancellationToken ct)
    {
        try
        {
            BankAdviceDto data = await sender.Send(new GetBankAdviceQuery(id), ct);
            byte[] xls = generator.Generate(data);
            return File(xls, "application/vnd.ms-excel", "Payroll_Bank_Statement.xls");
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}
