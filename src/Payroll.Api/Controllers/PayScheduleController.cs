using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.PaySchedule;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.PaySchedule;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/pay-schedule")]
[Authorize]
public sealed class PayScheduleController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        PayScheduleDto? dto = await sender.Send(new GetPayScheduleQuery(), cancellationToken);
        if (dto is null) return NoContent();
        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertPayScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new UpsertPayScheduleCommand(
                request.WorkWeekDays,
                request.SalaryCalculationMethod,
                request.FixedWorkingDaysPerMonth,
                request.PayDateType,
                request.PayDateDay,
                request.FirstPayPeriodMonth,
                request.FirstPayPeriodYear,
                GetActorId()),
                cancellationToken);

            return NoContent();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record UpsertPayScheduleRequest(
    List<string> WorkWeekDays,
    string SalaryCalculationMethod,
    int? FixedWorkingDaysPerMonth,
    string PayDateType,
    int? PayDateDay,
    int? FirstPayPeriodMonth,
    int? FirstPayPeriodYear);
