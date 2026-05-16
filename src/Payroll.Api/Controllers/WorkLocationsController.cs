using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.WorkLocations;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.WorkLocations;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/work-locations")]
[Authorize]
public sealed class WorkLocationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkLocationDto> locations =
            await sender.Send(new ListWorkLocationsQuery(), cancellationToken);
        return Ok(locations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            WorkLocationDto location = await sender.Send(new GetWorkLocationQuery(id), cancellationToken);
            return Ok(location);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateWorkLocationCommand(
                    request.Name,
                    request.State,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.PinCode,
                    actorId),
                cancellationToken);
            return Created($"/api/v1/work-locations/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWorkLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(
                new UpdateWorkLocationCommand(
                    id,
                    request.Name,
                    request.AddressLine1,
                    request.AddressLine2,
                    request.City,
                    request.PinCode,
                    actorId),
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
            await sender.Send(new DeleteWorkLocationCommand(id, actorId), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(new ToggleWorkLocationActiveCommand(id, false, actorId), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(new ToggleWorkLocationActiveCommand(id, true, actorId), cancellationToken);
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

public record CreateWorkLocationRequest(
    string Name,
    string State,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PinCode);

public record UpdateWorkLocationRequest(
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PinCode);
