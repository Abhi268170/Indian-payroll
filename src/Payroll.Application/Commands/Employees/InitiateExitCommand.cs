using FluentValidation;
using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine;
using System.Text.Json;

namespace Payroll.Application.Commands.Employees;

public record InitiateExitCommand(
    Guid EmployeeId,
    DateOnly LastWorkingDay,
    ExitReason Reason,
    ExitSettlementMode SettlementMode,
    DateOnly? SettlementDate,
    string? PersonalEmail,
    string? Notes,
    Guid ActorId) : IRequest<EmployeeExitDto>;

public sealed class InitiateExitCommandValidator : AbstractValidator<InitiateExitCommand>
{
    public InitiateExitCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.PersonalEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail))
            .WithMessage("Personal email is not a valid address.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.SettlementDate)
            .NotNull().When(x => x.SettlementMode == ExitSettlementMode.CustomDate)
            .WithMessage("Settlement date is required when Pay on a given date is selected.");
        RuleFor(x => x.SettlementDate)
            .Must((cmd, sd) => sd == null || sd >= cmd.LastWorkingDay)
            .WithMessage("Settlement date cannot be before the last working day.");
        RuleFor(x => x.LastWorkingDay)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-30))
            .WithMessage("Last working day cannot be more than 30 days in the past.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date).AddYears(5))
            .WithMessage("Last working day cannot be more than 5 years in the future.");
    }
}

public sealed class InitiateExitHandler(
    IEmployeeRepository employeeRepo,
    IEmployeeExitRepository exitRepo,
    IOrgProfileRepository orgProfileRepo,
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayScheduleRepository payScheduleRepo,
    IStatutoryConfigRepository statutoryRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<InitiateExitCommand, EmployeeExitDto>
{
    public async Task<EmployeeExitDto> Handle(InitiateExitCommand req, CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        if (employee.Status != EmployeeStatus.Active)
            throw new DomainException($"Cannot initiate exit: employee status is {employee.Status}.");

        var existingExit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct);
        if (existingExit != null)
            throw new DomainException("An exit is already in progress for this employee.");

        // Tax deductor gate: only blocks when the deductor was set to this employee
        // via the new DeductorEmployeeId FK. v1 leaves the FK null until the
        // Settings → Taxes page is rebuilt to mirror Zoho.
        var orgProfile = await orgProfileRepo.GetAsync(ct);
        if (orgProfile?.DeductorEmployeeId == req.EmployeeId)
            throw new DomainException(
                "Cannot initiate exit: this employee is the organisation's Tax Deductor. "
                + "Reassign in Settings → Taxes first.");

        // Resolve target pay date for the FnF run.
        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        DateOnly fnfPayDate = ResolveFnfPayDate(req, paySchedule);

        var exit = EmployeeExit.Create(
            employeeId: req.EmployeeId,
            lastWorkingDay: req.LastWorkingDay,
            reason: req.Reason,
            settlementMode: req.SettlementMode,
            settlementDate: req.SettlementMode == ExitSettlementMode.CustomDate ? req.SettlementDate : null,
            personalEmail: req.PersonalEmail,
            notes: req.Notes,
            createdBy: req.ActorId);

        await exitRepo.AddAsync(exit, ct);

        employee.ScheduleExit(req.LastWorkingDay, req.ActorId);
        if (!string.IsNullOrWhiteSpace(req.PersonalEmail)
            && string.IsNullOrWhiteSpace(employee.PersonalEmail))
        {
            employee.SetPersonalEmail(req.PersonalEmail, req.ActorId);
        }

        // Strip from any open Draft regular runs that would otherwise double-pay.
        var openDraftRuns = await runRepo.FindDraftRegularRunsCoveringDateAsync(req.LastWorkingDay, ct);
        foreach (var r in openDraftRuns)
        {
            var pe = await payrunEmpRepo.GetByRunAndEmployeeAsync(r.Id, req.EmployeeId, ct);
            if (pe != null) payrunEmpRepo.Remove(pe);
        }

        // Create or append to the FnF run.
        string snapshot = await BuildStatutoryConfigSnapshotAsync(ct);
        PayrollRun fnfRun = req.SettlementMode == ExitSettlementMode.CustomDate
            ? PayrollRun.CreateFinalSettlement(
                tenantId: tenantContext.TenantId,
                payPeriod: new PayPeriod(req.LastWorkingDay.Year, req.LastWorkingDay.Month),
                payDay: fnfPayDate,
                employeeExitId: exit.Id,
                statutoryConfigSnapshot: snapshot,
                createdBy: req.ActorId)
            : await GetOrCreateBulkFnfRunAsync(fnfPayDate, snapshot, req, ct);

        if (req.SettlementMode == ExitSettlementMode.CustomDate)
            await runRepo.AddAsync(fnfRun, ct);

        var payrunEmp = PayrunEmployee.Create(
            payrollRunId: fnfRun.Id,
            employeeId: req.EmployeeId,
            tenantId: tenantContext.TenantId,
            baseDays: DateTime.DaysInMonth(req.LastWorkingDay.Year, req.LastWorkingDay.Month),
            createdBy: req.ActorId,
            employeeExitId: exit.Id);
        await payrunEmpRepo.AddAsync(payrunEmp, ct);

        if (fnfRun.Type == PayrollRunType.BulkFinalSettlement)
            fnfRun.SetEmployeeCount(fnfRun.EmployeeCount + 1, req.ActorId);

        exit.LinkFnfRun(fnfRun.Id, req.ActorId);

        await uow.SaveChangesAsync(ct);

        return Map(exit, fnfRun);
    }

    private async Task<PayrollRun> GetOrCreateBulkFnfRunAsync(
        DateOnly payDate, string snapshot, InitiateExitCommand req, CancellationToken ct)
    {
        var existing = await runRepo.FindDraftBulkFnfByPayDateAsync(payDate, ct);
        if (existing != null) return existing;

        var fresh = PayrollRun.CreateBulkFinalSettlement(
            tenantId: tenantContext.TenantId,
            payPeriod: new PayPeriod(payDate.Year, payDate.Month),
            payDay: payDate,
            statutoryConfigSnapshot: snapshot,
            createdBy: req.ActorId);
        await runRepo.AddAsync(fresh, ct);
        return fresh;
    }

    private static DateOnly ResolveFnfPayDate(InitiateExitCommand req, Payroll.Domain.Entities.PaySchedule paySchedule)
    {
        if (req.SettlementMode == ExitSettlementMode.CustomDate)
            return req.SettlementDate!.Value;

        EnginePayDateType type = paySchedule.PayDateType == PayDateType.LastDay
            ? EnginePayDateType.LastDay
            : EnginePayDateType.SpecificDay;
        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        return PayScheduleHelpers.FirstRegularPayDateOnOrAfter(
            type, paySchedule.PayDateDay, req.LastWorkingDay, workWeek);
    }

    private async Task<string> BuildStatutoryConfigSnapshotAsync(CancellationToken ct)
    {
        // Lightweight snapshot for FnF runs: store the minimal JSON needed for the
        // orchestrator to compute. Full snapshot building is mirrored from
        // InitiatePayrollRunCommand and will be filled out by Phase 3.
        var orgConfig = await statutoryRepo.GetByTenantAsync(ct);
        return JsonSerializer.Serialize(new { OrgConfigId = orgConfig?.Id });
    }

    private static EmployeeExitDto Map(EmployeeExit e, PayrollRun fnfRun) =>
        new(
            Id: e.Id,
            EmployeeId: e.EmployeeId,
            LastWorkingDay: e.LastWorkingDay,
            Reason: e.Reason.ToString(),
            SettlementMode: e.SettlementMode.ToString(),
            SettlementDate: e.SettlementDate,
            PersonalEmail: e.PersonalEmail,
            Notes: e.Notes,
            FnfPayrollRunId: fnfRun.Id,
            FnfPayrollRunType: fnfRun.Type.ToString(),
            FnfPayDate: fnfRun.PayDay);
}
