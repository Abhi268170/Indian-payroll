using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgStructure;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.OrgStructure;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/designations")]
[Authorize]
public sealed class DesignationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<DesignationDto> designations =
            await sender.Send(new ListDesignationsQuery(), cancellationToken);
        return Ok(designations);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateDesignationCommand(request.Name, actorId),
                cancellationToken);
            return Created($"/api/v1/designations/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(
                new UpdateDesignationCommand(id, request.Name, actorId),
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
            await sender.Send(new DeleteDesignationCommand(id, actorId), cancellationToken);
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

public record CreateDesignationRequest(string Name);
public record UpdateDesignationRequest(string Name);
