using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.SalaryComponents;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.SalaryComponents;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/salary-components")]
[Authorize]
public sealed class SalaryComponentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        ComponentCategory? cat = null;
        if (!string.IsNullOrEmpty(category) && !Enum.TryParse(category, out ComponentCategory parsed))
            return BadRequest(new { error = "Invalid category value." });
        else if (!string.IsNullOrEmpty(category))
            cat = Enum.Parse<ComponentCategory>(category);

        var result = await sender.Send(
            new ListSalaryComponentsQuery(cat, new PaginationParams(page, pageSize)), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        SalaryComponentDetailDto? dto = await sender.Send(new GetSalaryComponentQuery(id), ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("active-earnings")]
    public async Task<IActionResult> ActiveEarnings(CancellationToken ct)
    {
        List<SalaryComponentSummaryDto> result = await sender.Send(new ListActiveEarningsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("active-benefits")]
    public async Task<IActionResult> ActiveBenefits(CancellationToken ct)
    {
        List<SalaryComponentSummaryDto> result = await sender.Send(new ListActiveBenefitsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("one-time")]
    public async Task<IActionResult> ListOneTime(
        [FromQuery] ComponentCategory category, CancellationToken ct)
    {
        if (category != ComponentCategory.Earning && category != ComponentCategory.Deduction)
            return BadRequest(new { error = "category must be 'Earning' or 'Deduction'." });

        List<SalaryComponentSummaryDto> result =
            await sender.Send(new ListOneTimeComponentsQuery(category), ct);
        return Ok(result);
    }

    [HttpPost("earnings")]
    public async Task<IActionResult> CreateEarning(
        [FromBody] CreateEarningRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateEarningCommand(
                req.Name, req.NameInPayslip, req.Code,
                req.EarningType, req.PayType, req.FormulaType,
                req.FixedAmount, req.Percentage,
                req.IsTaxable, req.ConsiderForEpf, req.EpfInclusionRule,
                req.ConsiderForEsi, req.CalculateOnProRata, req.ShowInPayslip,
                GetActorId(),
                IsOneTime: req.IsOneTime), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("deductions")]
    public async Task<IActionResult> CreateDeduction(
        [FromBody] CreateDeductionRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateDeductionCommand(
                req.Name, req.NameInPayslip, req.Code, req.DeductionFrequency,
                GetActorId(),
                IsOneTime: req.IsOneTime), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("reimbursements")]
    public async Task<IActionResult> CreateReimbursement(
        [FromBody] CreateReimbursementRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateReimbursementCommand(
                req.Name, req.NameInPayslip, req.Code,
                req.ReimbursementType, req.Amount, req.UnclaimedHandling,
                GetActorId()), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("benefits")]
    public async Task<IActionResult> CreateBenefit(
        [FromBody] CreateBenefitRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateBenefitCommand(
                req.Name, req.NameInPayslip, req.Code,
                req.BenefitType, req.BenefitPercentage,
                req.IsApplicableToAllEmployees, req.IsNpsGovernmentSector,
                GetActorId()), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("corrections")]
    public async Task<IActionResult> CreateCorrection(
        [FromBody] CreateCorrectionRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateCorrectionCommand(
                req.CorrectionName, req.Code, req.ForCorrectionOfComponentId,
                GetActorId()), ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSalaryComponentRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdateSalaryComponentCommand(
                id, req.Name, req.NameInPayslip,
                req.FormulaType, req.FixedAmount, req.Percentage,
                req.IsTaxable, req.ConsiderForEpf, req.EpfInclusionRule,
                req.ConsiderForEsi, req.CalculateOnProRata, req.ShowInPayslip,
                req.DeductionFrequency,
                req.ReimbursementAmount, req.UnclaimedHandling,
                GetActorId()), ct);
            return NoContent();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(
        Guid id, [FromBody] SetActiveRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new SetSalaryComponentActiveCommand(id, req.IsActive, GetActorId()), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
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

public record CreateEarningRequest(
    string Name, string NameInPayslip, string? Code,
    string EarningType, string PayType, string FormulaType,
    decimal? FixedAmount, decimal? Percentage,
    bool IsTaxable, bool ConsiderForEpf, string EpfInclusionRule,
    bool ConsiderForEsi, bool CalculateOnProRata, bool ShowInPayslip,
    bool IsOneTime = false);

public record CreateDeductionRequest(
    string Name, string NameInPayslip, string? Code, string DeductionFrequency,
    bool IsOneTime = false);

public record CreateReimbursementRequest(
    string Name, string NameInPayslip, string? Code,
    string ReimbursementType, decimal Amount, string UnclaimedHandling);

public record CreateBenefitRequest(
    string Name, string NameInPayslip, string? Code,
    string BenefitType, decimal? BenefitPercentage,
    bool IsApplicableToAllEmployees, bool? IsNpsGovernmentSector);

public record CreateCorrectionRequest(
    string CorrectionName, string? Code, Guid ForCorrectionOfComponentId);

public record UpdateSalaryComponentRequest(
    string Name, string NameInPayslip,
    string? FormulaType, decimal? FixedAmount, decimal? Percentage,
    bool? IsTaxable, bool? ConsiderForEpf, string? EpfInclusionRule,
    bool? ConsiderForEsi, bool? CalculateOnProRata, bool? ShowInPayslip,
    string? DeductionFrequency,
    decimal? ReimbursementAmount, string? UnclaimedHandling);

public record SetActiveRequest(bool IsActive);
