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
        Guid id, [FromBody] CreateSalaryStructureTemplateRequest req, CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdateSalaryStructureTemplateCommand(
                id, req.Name, req.Description,
                req.Components.Select(c => new TemplateComponentInput(
                    c.ComponentId, c.FormulaType, c.FixedAmount, c.Percentage, c.DisplayOrder))
                .ToList(),
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
    List<TemplateComponentInputRequest> Components);

public record TemplateComponentInputRequest(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    int DisplayOrder);
