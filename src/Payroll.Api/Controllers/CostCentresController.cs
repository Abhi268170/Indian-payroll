using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgStructure;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.OrgStructure;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/cost-centres")]
[Authorize]
public sealed class CostCentresController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<CostCentreDto> costCentres =
            await sender.Send(new ListCostCentresQuery(), cancellationToken);
        return Ok(costCentres);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCostCentreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateCostCentreCommand(request.Name, request.Code, actorId),
                cancellationToken);
            return Created($"/api/v1/cost-centres/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCostCentreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(
                new UpdateCostCentreCommand(id, request.Name, request.Code, actorId),
                cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(new DeleteCostCentreCommand(id, actorId), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record CreateCostCentreRequest(string Name, string? Code);
public record UpdateCostCentreRequest(string Name, string? Code);
