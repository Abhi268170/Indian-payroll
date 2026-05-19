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
            PayrollRunSummaryDto dto = await sender.Send(new InitiatePayrollRunCommand(GetActorId()), ct);
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

    [HttpGet("{id:guid}/employees")]
    public async Task<IActionResult> GetEmployees(Guid id, [FromQuery] string? filter, CancellationToken ct)
    {
        try
        {
            IReadOnlyList<PayrunEmployeeDto> list = await sender.Send(new GetPayrollRunEmployeesQuery(id, filter), ct);
            return Ok(list);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    [HttpGet("{id:guid}/employees/{eid:guid}/inputs")]
    public async Task<IActionResult> GetEmployeeInputs(Guid id, Guid eid, CancellationToken ct)
    {
        try
        {
            EmployeeVariableInputsDto dto = await sender.Send(new GetEmployeeVariableInputsQuery(id, eid), ct);
            return Ok(dto);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    [HttpPut("{id:guid}/employees/{eid:guid}/lop")]
    public async Task<IActionResult> SetLop(Guid id, Guid eid, [FromBody] SetLopRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new SetLopCommand(id, eid, req.LopDays, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/employees/{eid:guid}/earnings")]
    public async Task<IActionResult> AddOneTimeEarning(Guid id, Guid eid, [FromBody] AddOneTimeEarningRequest req, CancellationToken ct)
    {
        try
        {
            Guid breakdownId = await sender.Send(new AddOneTimeEarningCommand(id, eid, req.ComponentId, req.Amount, GetActorId()), ct);
            return Ok(new { id = breakdownId });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/employees/{eid:guid}/earnings/{breakdownId:guid}")]
    public async Task<IActionResult> RemoveOneTimeEarning(Guid id, Guid eid, Guid breakdownId, CancellationToken ct)
    {
        try
        {
            await sender.Send(new RemoveOneTimeEarningCommand(id, eid, breakdownId, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/employees/{eid:guid}/tds-override")]
    public async Task<IActionResult> OverrideTds(Guid id, Guid eid, [FromBody] OverrideTdsRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new OverrideTdsCommand(id, eid, req.OverrideAmount, req.Reason ?? string.Empty, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/employees/{eid:guid}/skip")]
    public async Task<IActionResult> SkipEmployee(Guid id, Guid eid, [FromBody] SkipEmployeeRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new SkipEmployeeCommand(id, eid, req.Reason, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/employees/{eid:guid}/skip")]
    public async Task<IActionResult> UndoSkipEmployee(Guid id, Guid eid, CancellationToken ct)
    {
        try
        {
            await sender.Send(new UndoSkipEmployeeCommand(id, eid, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/pending-tasks")]
    public async Task<IActionResult> GetPendingTasks(Guid id, CancellationToken ct)
    {
        try
        {
            PendingTasksDto dto = await sender.Send(new GetPendingTasksQuery(id), ct);
            return Ok(dto);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new ApprovePayrollRunCommand(id, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (PayrollRunHasBlockingTasksException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reject-approval")]
    public async Task<IActionResult> RejectApproval(Guid id, [FromBody] RejectApprovalRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new RejectApprovalCommand(id, req.Reason ?? string.Empty, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/record-payment")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new RecordPaymentCommand(id, req.PaymentDate, req.PaymentMode, req.Reference, req.NotifyEmployees, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/payment")]
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteRecordedPaymentCommand(id, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/employees/{eid:guid}/payslip")]
    public async Task<IActionResult> GetPayslipData(Guid id, Guid eid, CancellationToken ct)
    {
        try
        {
            PayslipData dto = await sender.Send(new GetPayslipDataQuery(id, eid), ct);
            return Ok(dto);
        }
        catch (NotFoundException) { return NotFound(); }
    }

    [HttpGet("{id:guid}/employees/{eid:guid}/payslip/pdf")]
    public async Task<IActionResult> GetPayslipPdf(Guid id, Guid eid, CancellationToken ct)
    {
        try
        {
            (Stream stream, string fileName) = await sender.Send(new GetPayslipPdfQuery(id, eid), ct);
            return File(stream, "application/pdf", fileName);
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/employees/{eid:guid}/payslip/send")]
    public async Task<IActionResult> SendPayslipEmail(Guid id, Guid eid, CancellationToken ct)
    {
        try
        {
            await sender.Send(new SendPayslipEmailCommand(id, eid), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
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

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        IReadOnlyList<PayrollHistoryItemDto> list = await sender.Send(new GetPayrollHistoryQuery(page, pageSize), ct);
        return Ok(list);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeletePayrollRunCommand(id, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/regenerate-payslips")]
    public async Task<IActionResult> RegeneratePayslips(Guid id, [FromServices] IPayrollJobDispatcher jobDispatcher, CancellationToken ct)
    {
        try
        {
            PayrollRunSummaryDto run = await sender.Send(new GetPayrollRunSummaryQuery(id), ct);
            if (run.Status != "Paid")
                return UnprocessableEntity(new { error = "Payslips can only be regenerated for Paid runs." });

            // TenantId extracted from JWT claims (sub/tenant_id)
            string? tenantClaim = User.FindFirst("tenant_id")?.Value;
            if (!Guid.TryParse(tenantClaim, out Guid tenantId))
                return Unauthorized();

            jobDispatcher.EnqueueGeneratePayslips(run.Id, tenantId);
            return Accepted();
        }
        catch (NotFoundException) { return NotFound(); }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

// Request body records
public record SetLopRequest(int LopDays);
public record AddOneTimeEarningRequest(Guid ComponentId, decimal Amount);
public record OverrideTdsRequest(decimal OverrideAmount, string? Reason);
public record SkipEmployeeRequest(string Reason);
public record RejectApprovalRequest(string? Reason);
public record RecordPaymentRequest(DateOnly PaymentDate, string PaymentMode, string? Reference, bool NotifyEmployees);
