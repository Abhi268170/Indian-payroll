using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgStructure;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.OrgStructure;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/business-units")]
[Authorize]
public sealed class BusinessUnitsController(ISender sender, IBusinessUnitRepository repo) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var bu = await repo.GetByIdAsync(id, cancellationToken);
        if (bu is null) return NotFound();
        return Ok(new BusinessUnitDto(bu.Id, bu.Name, bu.Description));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<BusinessUnitDto> businessUnits =
            await sender.Send(new ListBusinessUnitsQuery(), cancellationToken);
        return Ok(businessUnits);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBusinessUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateBusinessUnitCommand(request.Name, request.Description, actorId),
                cancellationToken);
            return Created($"/api/v1/business-units/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBusinessUnitRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(
                new UpdateBusinessUnitCommand(id, request.Name, request.Description, actorId),
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
            await sender.Send(new DeleteBusinessUnitCommand(id, actorId), cancellationToken);
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

public record CreateBusinessUnitRequest(string Name, string? Description);
public record UpdateBusinessUnitRequest(string Name, string? Description);
