using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Platform.Tenants;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Platform;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = "SuperAdmin")]
public sealed class TenantsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantDto> tenants = await sender.Send(new ListTenantsQuery(), cancellationToken);
        return Ok(tenants);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid id = await sender.Send(new CreateTenantCommand(request.DisplayName, request.Slug, request.AdminEmail), cancellationToken);
            return Created($"/api/tenants/{id}", new { id, displayName = request.DisplayName, slug = request.Slug });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException)
        {
            return Conflict(new { error = $"A tenant with slug '{request.Slug}' already exists." });
        }
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new SuspendTenantCommand(id), cancellationToken);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}

public record CreateTenantRequest(string DisplayName, string Slug, string AdminEmail);
