using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Extensions;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine;
using Payroll.Engine.Inputs;

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
    IWorkLocationRepository workLocationRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository salaryComponentRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IAuditLogRepository auditLogRepo,
    IEmployeeDocumentRepository documentRepo,
    IExitDocumentGenerator exitDocGenerator,
    IFileStorageService fileStorage,
    ITenantContext tenantContext,
    ILogger<InitiateExitHandler> logger,
    IUnitOfWork uow)
    : IRequestHandler<InitiateExitCommand, EmployeeExitDto>
{
    public async Task<EmployeeExitDto> Handle(InitiateExitCommand req, CancellationToken ct)
    {
        Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        if (employee.Status != EmployeeStatus.Active)
            throw new DomainException($"Cannot initiate exit: employee status is {employee.Status}.");

        // WI-02: salary structure must exist before FnF run can be created.
        EmployeeSalaryStructure salaryStructure = await salaryStructureRepo.GetActiveWithOverridesAsync(req.EmployeeId, ct)
            ?? throw new DomainException("Employee has no active salary structure. Assign one before initiating exit.");

        EmployeeExit? existingExit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct);
        if (existingExit != null)
            throw new DomainException("An exit is already in progress for this employee.");

        // Tax deductor gate: only blocks when the deductor was set to this employee
        // via the new DeductorEmployeeId FK. v1 leaves the FK null until the
        // Settings → Taxes page is rebuilt to mirror Zoho.
        Domain.Entities.OrgProfile? orgProfile = await orgProfileRepo.GetAsync(ct);
        if (orgProfile?.DeductorEmployeeId == req.EmployeeId)
            throw new DomainException(
                "Cannot initiate exit: this employee is the organisation's Tax Deductor. "
                + "Reassign in Settings → Taxes first.");

        // Resolve target pay date for the FnF run.
        Domain.Entities.PaySchedule paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        DateOnly fnfPayDate = ResolveFnfPayDate(req, paySchedule);

        EmployeeExit exit = EmployeeExit.Create(
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
        IReadOnlyList<PayrollRun> openDraftRuns = await runRepo.FindDraftRegularRunsCoveringDateAsync(req.LastWorkingDay, ct);
        foreach (PayrollRun r in openDraftRuns)
        {
            PayrunEmployee? pe = await payrunEmpRepo.GetByRunAndEmployeeAsync(r.Id, req.EmployeeId, ct);
            if (pe != null) payrunEmpRepo.Remove(pe);
        }

        // Create or append to the FnF run.
        DateOnly lwdPeriodStart = new(req.LastWorkingDay.Year, req.LastWorkingDay.Month, 1);
        (string snapshot, StatutoryConfig staticConfig) = await BuildStatutoryConfigSnapshotAsync(ct, employee, lwdPeriodStart);
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

        PayrunEmployee payrunEmp = PayrunEmployee.Create(
            payrollRunId: fnfRun.Id,
            employeeId: req.EmployeeId,
            tenantId: tenantContext.TenantId,
            baseDays: DateTime.DaysInMonth(req.LastWorkingDay.Year, req.LastWorkingDay.Month),
            createdBy: req.ActorId,
            employeeExitId: exit.Id);
        await payrunEmpRepo.AddAsync(payrunEmp, ct);

        // WI-01: seed recurring salary component breakdowns so the FnF engine
        // has non-zero inputs when UpdateFnfRunCommand triggers computation.
        await SeedRecurringComponentBreakdownsAsync(
            fnfRun.Id, payrunEmp, salaryStructure, staticConfig, req.ActorId, ct);

        // WI-11: bulk run employee-count increment is handled inside
        // GetOrCreateBulkFnfRunAsync (fresh run starts at 1 while tracked as
        // Added; an existing re-used run is incremented + explicitly Updated).
        // CustomDate FinalSettlement runs are created with count 1 already.

        exit.LinkFnfRun(fnfRun.Id, req.ActorId);

        // WI-29: lock the current-employer YTD basis at initiation so the FnF
        // TDS sweep is deterministic even if a prior-month run is approved later.
        // Fiscal year is the LWD month's FY (matches the orchestrator).
        int lwdFiscalYear = req.LastWorkingDay.Month >= 4 ? req.LastWorkingDay.Year : req.LastWorkingDay.Year - 1;
        Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)> ytdMap = await payrunEmpRepo.GetCurrentEmployerYtdAsync([req.EmployeeId], lwdFiscalYear, ct);
        ytdMap.TryGetValue(req.EmployeeId, out (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds) ytd);
        exit.SetYtdSnapshot(ytd.YtdGross, ytd.YtdTaxableGross, ytd.YtdTds, req.ActorId);

        // WI-28: audit trail for the exit event (HR/compliance hook).
        await auditLogRepo.AddAsync(AuditLog.Create(
            tenantId: tenantContext.TenantId,
            action: "ExitInitiated",
            entityType: nameof(EmployeeExit),
            entityId: exit.Id,
            performedBy: req.ActorId,
            newValue: JsonSerializer.Serialize(new
            {
                req.EmployeeId,
                LastWorkingDay = req.LastWorkingDay.ToString("yyyy-MM-dd"),
                Reason = req.Reason.ToString(),
                SettlementMode = req.SettlementMode.ToString(),
                FnfPayrollRunId = fnfRun.Id,
            })), ct);

        await uow.SaveChangesAsync(ct);

        // WI-31: generate the relieving/experience letter AFTER the exit is
        // committed, so an object-storage hiccup can't roll back or block the exit.
        // The generation is best-effort: a failure here must NOT fail the exit (which
        // is already committed), otherwise the API returns a generic 500 and the exit
        // appears to have "failed" to the user even though it succeeded.
        try
        {
            await GenerateRelievingLetterAsync(employee, exit, orgProfile?.CompanyName ?? "The Company", req.ActorId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Relieving letter generation failed for employee {EmployeeId} after exit {ExitId} was committed; exit succeeds regardless.", employee.Id, exit.Id);
        }

        return Map(exit, fnfRun);
    }

    private async Task GenerateRelievingLetterAsync(
        Employee employee, EmployeeExit exit, string companyName, Guid actorId, CancellationToken ct)
    {
        Domain.ValueObjects.Tenure tenure = employee.TenureAt(exit.LastWorkingDay);
        string tenureLabel = $"{tenure.Years}y {tenure.Months}m";

        byte[] pdf = exitDocGenerator.GenerateRelievingLetter(employee, exit, companyName, tenureLabel);
        string storageKey = $"exit-documents/{tenantContext.TenantId}/{employee.Id}/relieving-letter-{exit.Id}.pdf";
        using (MemoryStream ms = new(pdf))
            await fileStorage.UploadAsync(storageKey, ms, "application/pdf", ct);

        await documentRepo.AddAsync(EmployeeDocument.Create(
            employeeId: employee.Id,
            tenantId: tenantContext.TenantId,
            documentType: "RelievingLetter",
            fileName: $"Relieving_Letter_{employee.EmployeeCode}.pdf",
            storageKey: storageKey,
            createdBy: actorId), ct);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<PayrollRun> GetOrCreateBulkFnfRunAsync(
        DateOnly payDate, string snapshot, InitiateExitCommand req, CancellationToken ct)
    {
        PayrollRun? existing = await runRepo.FindDraftBulkFnfByPayDateAsync(payDate, ct);
        if (existing != null)
        {
            // WI-11: appending another employee to a re-used bulk run. The entity
            // came from a query, so mark it explicitly so the count increment
            // persists under the repository pattern.
            MergeSnapshotForAppendedEmployee(existing, snapshot, req.ActorId);
            existing.SetEmployeeCount(existing.EmployeeCount + 1, req.ActorId);
            runRepo.Update(existing);
            return existing;
        }

        PayrollRun fresh = PayrollRun.CreateBulkFinalSettlement(
            tenantId: tenantContext.TenantId,
            payPeriod: new PayPeriod(payDate.Year, payDate.Month),
            payDay: payDate,
            statutoryConfigSnapshot: snapshot,
            createdBy: req.ActorId);
        // First employee in this bulk run. Set count in-memory before AddAsync
        // so the INSERT carries count = 1 (no Update on an Added entity).
        fresh.SetEmployeeCount(1, req.ActorId);
        await runRepo.AddAsync(fresh, ct);
        return fresh;
    }

    // A bulk FnF run's snapshot was historically built from the FIRST exiting
    // employee only — a second employee in another state silently got PT = 0
    // (no slabs for their state) and possibly another FY's tax config. Appending
    // now merges the new employee's PT/LWF state data and refuses to mix
    // employees whose LWDs fall under different income-tax configs.
    private void MergeSnapshotForAppendedEmployee(PayrollRun existing, string newSnapshotJson, Guid actorId)
    {
        if (existing.StatutoryConfigSnapshot is null)
        {
            existing.UpdateStatutoryConfigSnapshot(newSnapshotJson, actorId);
            return;
        }

        StatutoryConfig current = JsonSerializer.Deserialize<StatutoryConfig>(existing.StatutoryConfigSnapshot)!;
        StatutoryConfig incoming = JsonSerializer.Deserialize<StatutoryConfig>(newSnapshotJson)!;

        // Legacy/blank snapshot (no slabs): adopt the incoming one wholesale.
        if (current.NewRegimeSlabs is null || current.NewRegimeSlabs.Count == 0)
        {
            existing.UpdateStatutoryConfigSnapshot(newSnapshotJson, actorId);
            return;
        }

        bool sameTaxBasis =
            current.StandardDeduction == incoming.StandardDeduction
            && current.Rebate87ALimit == incoming.Rebate87ALimit
            && current.Rebate87AAmount == incoming.Rebate87AAmount
            && current.CessRate == incoming.CessRate
            && current.NewRegimeSlabs.Count == incoming.NewRegimeSlabs.Count
            && current.NewRegimeSlabs.Zip(incoming.NewRegimeSlabs)
                .All(p => p.First == p.Second);
        if (!sameTaxBasis)
            throw new DomainException(
                "This bulk settlement run was built with a different fiscal year's tax configuration. " +
                "Use a custom settlement date to create a separate run for this employee.");

        HashSet<string> knownPtStates = current.PTSlabs.Select(p => p.StateCode).ToHashSet();
        HashSet<string> knownLwfStates = current.LWFStates.Select(l => l.StateCode).ToHashSet();
        List<PTSlab> mergedPt = [.. current.PTSlabs, .. incoming.PTSlabs.Where(p => !knownPtStates.Contains(p.StateCode))];
        List<LwfStateInput> mergedLwf = [.. current.LWFStates, .. incoming.LWFStates.Where(l => !knownLwfStates.Contains(l.StateCode))];

        if (mergedPt.Count != current.PTSlabs.Count || mergedLwf.Count != current.LWFStates.Count)
        {
            StatutoryConfig merged = current with { PTSlabs = mergedPt, LWFStates = mergedLwf };
            existing.UpdateStatutoryConfigSnapshot(JsonSerializer.Serialize(merged), actorId);
        }
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

    private async Task<(string Json, StatutoryConfig Config)> BuildStatutoryConfigSnapshotAsync(
        CancellationToken ct, Employee employee, DateOnly periodStart)
    {
        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? throw new DomainException("Statutory configuration not found. Configure EPF/ESI settings first.");

        int fiscalYear = periodStart.Month >= 4 ? periodStart.Year : periodStart.Year - 1;
        string fyLabel = Payroll.Domain.ValueObjects.PayPeriod.FiscalYearKeyFor(fiscalYear);
        IncomeTaxConfig? taxConfig = await statutoryRepo.GetIncomeTaxConfigAsync(fyLabel, "New", ct);
        IReadOnlyList<IncomeTaxSlab> taxSlabs = await statutoryRepo.GetIncomeTaxSlabsAsync(fyLabel, "New", ct);
        IReadOnlyList<IncomeTaxSurchargeSlab> surchargeSlabs = await statutoryRepo.GetSurchargeSlabsAsync(fyLabel, "New", ct);

        WorkLocation? workLocation = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
        string stateCode = workLocation?.State.ToIsoCode() ?? "MH";
        IReadOnlyList<ProfessionalTaxSlab> ptSlabs = await statutoryRepo.GetPtSlabsAsync(stateCode, periodStart, ct);
        IReadOnlyList<LwfStateConfig> lwfConfigs = await statutoryRepo.GetLwfConfigsAsync(new[] { stateCode }, ct);

        StatutoryConfig staticConfig = StatutoryConfigBuilder.Build(orgConfig, taxConfig, taxSlabs, surchargeSlabs, ptSlabs, lwfConfigs);
        return (JsonSerializer.Serialize(staticConfig), staticConfig);
    }

    private async Task SeedRecurringComponentBreakdownsAsync(
        Guid fnfRunId,
        PayrunEmployee payrunEmp,
        EmployeeSalaryStructure salaryStructure,
        StatutoryConfig staticConfig,
        Guid actorId,
        CancellationToken ct)
    {
        SalaryStructureTemplate? template = salaryStructure.SalaryStructureTemplateId.HasValue
            ? await templateRepo.GetByIdWithComponentsAsync(salaryStructure.SalaryStructureTemplateId.Value, ct)
            : null;

        // Identify override-only component IDs not present in the template.
        HashSet<Guid> addedComponentIds = new HashSet<Guid>();
        if (salaryStructure.ComponentOverrides.Count > 0 && template is not null)
        {
            HashSet<Guid> templateCompIds = new HashSet<Guid>(template.Components.Select(c => c.ComponentId));
            foreach (EmployeeSalaryComponentOverride ov in salaryStructure.ComponentOverrides)
            {
                if (!templateCompIds.Contains(ov.SalaryComponentId))
                    addedComponentIds.Add(ov.SalaryComponentId);
            }
        }

        Dictionary<Guid, SalaryComponent> addedCompDetails = addedComponentIds.Count > 0
            ? (await salaryComponentRepo.GetByIdsAsync([.. addedComponentIds], ct)).ToDictionary(c => c.Id)
            : [];

        InitiatePayrollRunHandler.ComponentBuildResult build =
            InitiatePayrollRunHandler.BuildComponentInputs(salaryStructure, template, addedCompDetails, staticConfig);
        IReadOnlyList<SalaryComponentInput> components = build.Components;

        payrunEmp.SetMonthlyCTC(salaryStructure.AnnualCTC / 12m, actorId);
        if (build.VpfPercent > 0m)
            payrunEmp.SetVpfPercent(build.VpfPercent, actorId);

        decimal monthlyBasic = components.FirstOrDefault(c => c.Code == "BASICSALARY")?.Amount ?? 0m;

        foreach (SalaryComponentInput comp in components)
        {
            PayrunComponentBreakdown breakdown = PayrunComponentBreakdown.Create(
                payrollRunId: fnfRunId,
                employeeId: payrunEmp.EmployeeId,
                tenantId: payrunEmp.TenantId,
                salaryComponentId: comp.ComponentId == Guid.Empty ? null : comp.ComponentId,
                componentCode: comp.Code,
                componentName: comp.Code,
                fullAmount: comp.Amount,
                proratedAmount: comp.Amount,
                isOneTimeEarning: false,
                isTaxable: comp.IsTaxable,
                considerForEpf: comp.ConsiderForEpf,
                considerForEsi: comp.ConsiderForEsi,
                calculateOnProRata: comp.CalculateOnProRata,
                showInPayslip: comp.ShowInPayslip);
            await breakdownRepo.AddAsync(breakdown, ct);
        }

        // WI-17: persist employer-borne benefit-category overrides (health
        // insurance, NPS employer match, etc.) as IsBenefit rows so the FnF
        // payslip renders the "Employer benefits" section. These are netted out
        // of CTC by BuildComponentInputs and must NOT flow into gross — the
        // orchestrator excludes IsBenefit rows from engine inputs.
        await SeedBenefitBreakdownsAsync(fnfRunId, payrunEmp, salaryStructure, template,
            addedCompDetails, monthlyBasic, actorId, ct);
    }

    private async Task SeedBenefitBreakdownsAsync(
        Guid fnfRunId,
        PayrunEmployee payrunEmp,
        EmployeeSalaryStructure salaryStructure,
        SalaryStructureTemplate? template,
        Dictionary<Guid, SalaryComponent> addedCompDetails,
        decimal monthlyBasic,
        Guid actorId,
        CancellationToken ct)
    {
        HashSet<Guid> templateCompIds = template?.Components.Select(c => c.ComponentId).ToHashSet() ?? [];

        foreach (EmployeeSalaryComponentOverride ov in salaryStructure.ComponentOverrides)
        {
            if (templateCompIds.Contains(ov.SalaryComponentId)) continue;
            if (!addedCompDetails.TryGetValue(ov.SalaryComponentId, out SalaryComponent? sc)) continue;
            if (sc.Category != ComponentCategory.Benefit) continue;

            decimal benefitMonthly = ov.FormulaType switch
            {
                ComponentFormulaType.Fixed => ov.FixedAmount ?? 0m,
                ComponentFormulaType.PercentOfCTC =>
                    Math.Round(salaryStructure.AnnualCTC * (ov.Percentage ?? 0m) / 100m / 12m, 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfBasic =>
                    Math.Round(monthlyBasic * (ov.Percentage ?? 0m) / 100m, 2, MidpointRounding.AwayFromZero),
                _ => 0m,
            };
            if (benefitMonthly <= 0m) continue;

            PayrunComponentBreakdown benefitRow = PayrunComponentBreakdown.Create(
                payrollRunId: fnfRunId,
                employeeId: payrunEmp.EmployeeId,
                tenantId: payrunEmp.TenantId,
                salaryComponentId: sc.Id,
                componentCode: sc.Code,
                componentName: sc.NameInPayslip,
                fullAmount: benefitMonthly,
                proratedAmount: benefitMonthly,
                isOneTimeEarning: false,
                isTaxable: false,
                considerForEpf: false,
                considerForEsi: false,
                calculateOnProRata: false,
                showInPayslip: sc.ShowInPayslip ?? true,
                isBenefit: true);
            await breakdownRepo.AddAsync(benefitRow, ct);
        }
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
