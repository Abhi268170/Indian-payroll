using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgStructure;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.OrgStructure;
using Payroll.Domain.Enums;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/org")]
[Authorize(Policy = "OrgAdmin")]
public sealed class OrgStructureController(ISender sender) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<IActionResult> ListBranches(CancellationToken cancellationToken)
    {
        IReadOnlyList<BranchDto> result = await sender.Send(new ListBranchesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateBranchCommand(request.Name, request.State, actorId), cancellationToken);
            return Created($"/api/org/branches/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments(CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentDto> result = await sender.Send(new ListDepartmentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateDepartmentCommand(request.Name, request.Code, actorId), cancellationToken);
            return Created($"/api/org/departments/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("designations")]
    public async Task<IActionResult> ListDesignations(CancellationToken cancellationToken)
    {
        IReadOnlyList<DesignationDto> result = await sender.Send(new ListDesignationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("designations")]
    public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateDesignationCommand(request.Name, actorId), cancellationToken);
            return Created($"/api/org/designations/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("cost-centres")]
    public async Task<IActionResult> ListCostCentres(CancellationToken cancellationToken)
    {
        IReadOnlyList<CostCentreDto> result = await sender.Send(new ListCostCentresQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("cost-centres")]
    public async Task<IActionResult> CreateCostCentre([FromBody] CreateCostCentreRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateCostCentreCommand(request.Name, request.Code, actorId), cancellationToken);
            return Created($"/api/org/cost-centres/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record CreateBranchRequest(string Name, IndianState State);
public record CreateDepartmentRequest(string Name, string? Code);
public record CreateDesignationRequest(string Name);
public record CreateCostCentreRequest(string Name, string? Code);
