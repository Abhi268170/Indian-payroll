using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.SalaryStructureTemplates;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.SalaryStructureTemplates;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/salary-structure-templates")]
[Authorize]
public sealed class SalaryStructureTemplatesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        List<SalaryStructureTemplateSummaryDto> result =
            await sender.Send(new ListSalaryStructureTemplatesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        SalaryStructureTemplateDetailDto? dto =
            await sender.Send(new GetSalaryStructureTemplateQuery(id), ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalaryStructureTemplateRequest req, CancellationToken ct)
    {
        try
        {
            Guid id = await sender.Send(new CreateSalaryStructureTemplateCommand(
                req.Name, req.Description,
                req.Components.Select(c => new TemplateComponentInput(
                    c.ComponentId, c.FormulaType, c.FixedAmount, c.Percentage, c.DisplayOrder))
                .ToList(),
                GetActorId(),
                EpfEnabled: req.EpfEnabled,
                EsiEnabled: req.EsiEnabled,
                PtEnabled: req.PtEnabled,
                LwfEnabled: req.LwfEnabled), ct);
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
        Guid id, [FromBody] CreateSalaryStructureTemplateRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdateSalaryStructureTemplateCommand(
                id, req.Name, req.Description,
                req.Components.Select(c => new TemplateComponentInput(
                    c.ComponentId, c.FormulaType, c.FixedAmount, c.Percentage, c.DisplayOrder))
                .ToList(),
                GetActorId(),
                EpfEnabled: req.EpfEnabled,
                EsiEnabled: req.EsiEnabled,
                PtEnabled: req.PtEnabled,
                LwfEnabled: req.LwfEnabled), ct);
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

    // Backend-authoritative preview of an unsaved or saved salary structure.
    // Replaces three drifted client-side calculators with one round trip.
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(
        [FromBody] SalaryStructurePreviewRequest req, CancellationToken ct)
    {
        SalaryStructurePreviewDto result = await sender.Send(new SalaryStructurePreviewQuery(
            AnnualCtc: req.AnnualCtc,
            TemplateComponents: req.TemplateComponents.Select(c => new PreviewTemplateComponentInput(
                c.ComponentId, c.FormulaType, c.FixedAmount, c.Percentage, c.DisplayOrder)).ToList(),
            Overrides: (req.Overrides ?? []).Select(o => new PreviewOverrideInput(
                o.SalaryComponentId, o.FormulaType, o.FixedAmount, o.Percentage)).ToList(),
            AddedComponents: (req.AddedComponents ?? []).Select(a => new PreviewAddedComponentInput(
                a.ComponentId, a.FormulaType, a.FixedAmount, a.Percentage)).ToList(),
            EmployeeFlags: new PreviewEmployeeFlagsInput(
                EpfEnabled: req.EmployeeFlags?.EpfEnabled ?? true,
                EsiEnabled: req.EmployeeFlags?.EsiEnabled ?? true,
                PtEnabled: req.EmployeeFlags?.PtEnabled ?? true,
                LwfEnabled: req.EmployeeFlags?.LwfEnabled ?? true,
                GratuityEnabled: req.EmployeeFlags?.GratuityEnabled ?? true)
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(
        Guid id, [FromBody] SetActiveRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(
                new SetSalaryStructureTemplateActiveCommand(id, req.IsActive, GetActorId()), ct);
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

public record CreateSalaryStructureTemplateRequest(
    string Name,
    string? Description,
    List<TemplateComponentInputRequest> Components,
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true);

public record TemplateComponentInputRequest(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    int DisplayOrder);

public sealed record SalaryStructurePreviewRequest(
    decimal AnnualCtc,
    List<TemplateComponentInputRequest> TemplateComponents,
    List<PreviewOverrideRequest>? Overrides,
    List<PreviewAddedComponentRequest>? AddedComponents,
    PreviewEmployeeFlagsRequest? EmployeeFlags);

public sealed record PreviewOverrideRequest(
    Guid SalaryComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage);

public sealed record PreviewAddedComponentRequest(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage);

public sealed record PreviewEmployeeFlagsRequest(
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true,
    bool GratuityEnabled = true);
