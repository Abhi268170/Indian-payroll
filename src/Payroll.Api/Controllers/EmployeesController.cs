using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Employees;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Employees;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        IReadOnlyList<EmployeeListItemDto> items =
            await sender.Send(new ListEmployeesQuery(), ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        try
        {
            EmployeeDto dto = await sender.Send(new GetEmployeeQuery(id), ct);
            return Ok(dto);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest req,
        CancellationToken ct)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(new CreateEmployeeCommand(
                req.FirstName,
                req.MiddleName,
                req.LastName,
                req.EmployeeCode,
                req.WorkEmail,
                req.MobileNumber,
                req.Gender,
                req.DateOfJoining,
                req.DateOfBirth,
                req.EmploymentType,
                req.IsDirector,
                req.EnablePortalAccess,
                req.DepartmentId,
                req.DesignationId,
                req.WorkLocationId,
                req.BusinessUnitId,
                actorId), ct);
            return Created($"/api/v1/employees/{id}", new { id });
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}/basic-details")]
    public async Task<IActionResult> UpdateBasicDetails(
        Guid id,
        [FromBody] UpdateBasicDetailsRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdateBasicDetailsCommand(
                id, req.FirstName, req.MiddleName, req.LastName,
                req.MobileNumber, req.Gender, req.IsDirector, req.EnablePortalAccess,
                req.DepartmentId, req.DesignationId, req.WorkLocationId,
                req.BusinessUnitId, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}/personal-details")]
    public async Task<IActionResult> UpdatePersonalDetails(
        Guid id,
        [FromBody] UpdatePersonalDetailsRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdatePersonalDetailsCommand(
                id, req.DateOfBirth, req.FathersName, req.PAN, req.Aadhaar,
                req.PersonalEmail, req.DifferentlyAbledType, req.AddressLine1, req.AddressLine2,
                req.City, req.ResidentialState, req.PinCode, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}/payment-info")]
    public async Task<IActionResult> UpdatePaymentInfo(
        Guid id,
        [FromBody] UpdatePaymentInfoRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdatePaymentInfoCommand(
                id, req.PaymentMode, req.AccountHolderName, req.BankName,
                req.AccountType, req.AccountNumber, req.IFSC, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("{id:guid}/salary-structure")]
    public async Task<IActionResult> AssignSalaryStructure(
        Guid id,
        [FromBody] AssignSalaryStructureRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new AssignSalaryStructureCommand(
                id, req.AnnualCTC, req.SalaryStructureTemplateId,
                req.EpfEnabled, req.EsiEnabled, req.PtEnabled, req.LwfEnabled,
                GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("{id:guid}/salary-structure")]
    public async Task<IActionResult> GetSalaryStructure(Guid id, CancellationToken ct)
    {
        EmployeeSalaryStructureDto? dto =
            await sender.Send(new GetEmployeeSalaryStructureQuery(id), ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("{id:guid}/statutory-details")]
    public async Task<IActionResult> UpdateStatutoryDetails(
        Guid id,
        [FromBody] UpdateStatutoryDetailsRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpdateStatutoryDetailsCommand(
                id, req.EpfEnabled, req.EsiEnabled, req.PtEnabled, req.LwfEnabled,
                req.UAN, req.ESICIPNumber, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record CreateEmployeeRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? EmployeeCode,
    string WorkEmail,
    string? MobileNumber,
    string Gender,
    string DateOfJoining,
    string DateOfBirth,
    string EmploymentType,
    bool IsDirector,
    bool EnablePortalAccess,
    Guid DepartmentId,
    Guid DesignationId,
    Guid WorkLocationId,
    Guid? BusinessUnitId);

public record UpdateBasicDetailsRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? MobileNumber,
    string Gender,
    bool IsDirector,
    bool EnablePortalAccess,
    Guid DepartmentId,
    Guid DesignationId,
    Guid WorkLocationId,
    Guid? BusinessUnitId);

public record UpdatePersonalDetailsRequest(
    string? DateOfBirth,
    string? FathersName,
    string? PAN,
    string? Aadhaar,
    string? PersonalEmail,
    string DifferentlyAbledType,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ResidentialState,
    string? PinCode);

public record UpdatePaymentInfoRequest(
    string PaymentMode,
    string? AccountHolderName,
    string? BankName,
    string? AccountType,
    string? AccountNumber,
    string? IFSC);

public record UpdateStatutoryDetailsRequest(
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled,
    string? UAN,
    string? ESICIPNumber);

public record AssignSalaryStructureRequest(
    decimal AnnualCTC,
    Guid? SalaryStructureTemplateId,
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled);
