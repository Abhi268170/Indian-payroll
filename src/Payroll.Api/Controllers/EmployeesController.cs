using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgStructure;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Employees;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Policy = "HRManager")]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployeeDto> result = await sender.Send(new ListEmployeesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            EmployeeDto employee = await sender.Send(new GetEmployeeQuery(id), cancellationToken);
            return Ok(employee);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "HRManager")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateEmployeeCommand(
                request.FirstName,
                request.LastName,
                request.EmployeeCode,
                request.PAN,
                request.DateOfBirth,
                request.Gender,
                request.DateOfJoining,
                request.WorkState,
                request.EmploymentType,
                request.DepartmentId,
                request.DesignationId,
                actorId,
                request.BranchId,
                request.CostCentreId), cancellationToken);
            return Created($"/api/employees/{id}", new { id });
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

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string EmployeeCode,
    string PAN,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly DateOfJoining,
    IndianState WorkState,
    EmploymentType EmploymentType,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? BranchId,
    Guid? CostCentreId);
