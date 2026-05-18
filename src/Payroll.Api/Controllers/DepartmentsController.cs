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
[Route("api/v1/departments")]
[Authorize]
public sealed class DepartmentsController(ISender sender, IDepartmentRepository repo) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dept = await repo.GetByIdAsync(id, cancellationToken);
        if (dept is null) return NotFound();
        return Ok(new DepartmentDto(dept.Id, dept.Name, dept.Code, dept.Description));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentDto> departments =
            await sender.Send(new ListDepartmentsQuery(), cancellationToken);
        return Ok(departments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateDepartmentCommand(request.Name, request.Code, request.Description, actorId),
                cancellationToken);
            return Created($"/api/v1/departments/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            await sender.Send(
                new UpdateDepartmentCommand(id, request.Name, request.Code, request.Description, actorId),
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
            await sender.Send(new DeleteDepartmentCommand(id, actorId), cancellationToken);
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

public record CreateDepartmentRequest(string Name, string? Code, string? Description);
public record UpdateDepartmentRequest(string Name, string? Code, string? Description);
