using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Employees;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Application.Queries.Employees;
using Payroll.Application.Queries.Payslips;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Api.Controllers;

public sealed record UpsertFyOpeningRequest(int MonthsCount, decimal GrossSalary, decimal TdsDeducted, decimal PfDeducted);

public sealed record InitiateExitRequest(
    DateOnly LastWorkingDay,
    string Reason,
    string SettlementMode,
    DateOnly? SettlementDate,
    string? PersonalEmail,
    string? Notes);

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public sealed class EmployeesController(ISender sender, IEmployeeImportTemplateGenerator templateGenerator) : ControllerBase
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
                req.PersonalEmail, req.DifferentlyAbledType, req.IsPWD, req.AddressLine1, req.AddressLine2,
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
                req.Overrides?.Select(o => new ComponentOverrideInput(o.SalaryComponentId, o.FormulaType, o.Percentage, o.FixedAmount)).ToList()
                    ?? [],
                GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException) { return NotFound(); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("{id:guid}/payslips")]
    public async Task<IActionResult> GetPayslips(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetEmployeePayslipsQuery(id), ct));

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
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("{id:guid}/fy-opening/{fiscalYear:int}")]
    public async Task<IActionResult> GetFyOpening(Guid id, int fiscalYear, CancellationToken ct)
    {
        EmployeeFyOpeningDto? dto = await sender.Send(new GetEmployeeFyOpeningQuery(id, fiscalYear), ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("{id:guid}/fy-opening/{fiscalYear:int}")]
    public async Task<IActionResult> UpsertFyOpening(
        Guid id,
        int fiscalYear,
        [FromBody] UpsertFyOpeningRequest req,
        CancellationToken ct)
    {
        try
        {
            await sender.Send(new UpsertEmployeeFyOpeningCommand(
                id, fiscalYear, req.MonthsCount,
                req.GrossSalary, req.TdsDeducted, req.PfDeducted, GetActorId()), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPost("{id:guid}/exit")]
    public async Task<IActionResult> InitiateExit(Guid id, [FromBody] InitiateExitRequest req, CancellationToken ct)
    {
        try
        {
            EmployeeExitDto dto = await sender.Send(new InitiateExitCommand(
                EmployeeId: id,
                LastWorkingDay: req.LastWorkingDay,
                Reason: Enum.Parse<Payroll.Domain.Enums.ExitReason>(req.Reason),
                SettlementMode: Enum.Parse<Payroll.Domain.Enums.ExitSettlementMode>(req.SettlementMode),
                SettlementDate: req.SettlementDate,
                PersonalEmail: req.PersonalEmail,
                Notes: req.Notes,
                ActorId: GetActorId()), ct);
            return Ok(dto);
        }
        catch (NotFoundException) { return NotFound(); }
        catch (DomainException ex) { return UnprocessableEntity(new { error = ex.Message }); }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("import/template")]
    [AllowAnonymous] // blank template, no tenant data
    public IActionResult DownloadTemplate()
    {
        byte[] bytes = templateGenerator.Generate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "employee-import-template.xlsx");
    }

    [HttpPost("import/validate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ValidateImport(
        IFormFile file,
        [FromForm] bool overwriteExisting,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            using Stream stream = file.OpenReadStream();
            Application.Interfaces.ImportValidationResult result =
                await sender.Send(new ValidateEmployeeImportCommand(stream, overwriteExisting, GetActorId()), ct);
            return Ok(result);
        }
        catch (Domain.Common.ImportFormatException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("import/commit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CommitImport(
        IFormFile file,
        [FromForm] bool overwriteExisting,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            using Stream stream = file.OpenReadStream();
            CommitImportResult result =
                await sender.Send(new CommitEmployeeImportCommand(stream, overwriteExisting, GetActorId()), ct);
            return Ok(result);
        }
        catch (Domain.Common.ImportFormatException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
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
    bool IsPWD,
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

public record ComponentOverrideRequest(
    Guid SalaryComponentId,
    string FormulaType,
    decimal? Percentage,
    decimal? FixedAmount);

public record AssignSalaryStructureRequest(
    decimal AnnualCTC,
    Guid? SalaryStructureTemplateId,
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled,
    IReadOnlyList<ComponentOverrideRequest>? Overrides);
