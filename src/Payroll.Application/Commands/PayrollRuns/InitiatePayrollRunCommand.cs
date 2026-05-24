using FluentValidation;
using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Extensions;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine;
using Payroll.Engine.Inputs;
using System.Text.Json;

namespace Payroll.Application.Commands.PayrollRuns;

public record InitiatePayrollRunCommand(Guid ActorId) : IRequest<PayrollRunSummaryDto>;

internal sealed class InitiatePayrollRunCommandValidator : AbstractValidator<InitiatePayrollRunCommand>
{
    public InitiatePayrollRunCommandValidator()
    {
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class InitiatePayrollRunHandler(
    IPayScheduleRepository payScheduleRepo,
    IPayrollRunRepository payrollRunRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository salaryComponentRepo,
    IStatutoryConfigRepository statutoryRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IWorkLocationRepository workLocationRepo,
    IPriorEmployerYtdRepository priorYtdRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IPayrollCostCalculator costCalculator,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<InitiatePayrollRunCommand, PayrollRunSummaryDto>
{
    public async Task<PayrollRunSummaryDto> Handle(InitiatePayrollRunCommand req, CancellationToken ct)
    {
        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured. Configure Pay Schedule before initiating a payroll run.");

        // Determine next payable period
        var latestPaid = await payrollRunRepo.GetLatestPaidAsync(ct);
        PayPeriod period;
        if (latestPaid is not null)
        {
            var next = latestPaid.PayPeriod.StartDate.AddMonths(1);
            period = new PayPeriod(next.Year, next.Month);
        }
        else if (paySchedule.FirstPayPeriodMonth.HasValue && paySchedule.FirstPayPeriodYear.HasValue)
        {
            period = new PayPeriod(paySchedule.FirstPayPeriodYear.Value, paySchedule.FirstPayPeriodMonth.Value);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            period = new PayPeriod(now.Year, now.Month);
        }

        // Guard: no run already exists for period
        bool exists = await payrollRunRepo.ExistsForPeriodAsync(period, ct);
        if (exists)
            throw new DomainException($"A payroll run already exists for {period}. Delete or complete it before initiating a new one.");

        // Build StatutoryConfig snapshot
        var orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? throw new DomainException("Statutory configuration not found. Configure EPF/ESI settings first.");

        string fiscalYear = $"{period.FiscalYear}-{(period.FiscalYear + 1) % 100:D2}";
        var taxConfig = await statutoryRepo.GetIncomeTaxConfigAsync(fiscalYear, "New", ct);
        var taxSlabs = await statutoryRepo.GetIncomeTaxSlabsAsync(fiscalYear, "New", ct);
        var surchargeSlabs = await statutoryRepo.GetSurchargeSlabsAsync(fiscalYear, "New", ct);

        var employees = await employeeRepo.ListAsync(ct);
        // Exclude employees whose last working day already falls in or before this
        // pay period — their pay flows through a Final Settlement / Bulk Final
        // Settlement run, never through the regular run. Status==Active alone is
        // insufficient because the Exited flip happens overnight (MarkExitedOnLwdJob),
        // leaving a window where DateOfLeaving is set but status is still Active.
        DateOnly periodEnd = period.EndDate;
        var activeEmployees = employees
            .Where(e => e.Status == EmployeeStatus.Active
                && (e.DateOfLeaving == null || e.DateOfLeaving > periodEnd))
            .ToList();

        var workLocations = await workLocationRepo.ListAsync(ct);
        var workLocationStateMap = workLocations.ToDictionary(wl => wl.Id, wl => wl.State.ToIsoCode());

        // Resolve pay day and salary divisor from pay schedule settings
        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        EnginePayDateType payDateType = paySchedule.PayDateType == PayDateType.LastDay
            ? EnginePayDateType.LastDay
            : EnginePayDateType.SpecificDay;
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        DateOnly payDay = PayScheduleHelpers.ResolveActualPayDate(payDateType, paySchedule.PayDateDay,
            period.Year, period.Month, workWeek);

        int calendarDays = DateTime.DaysInMonth(period.Year, period.Month);
        int salaryDivisor = PayScheduleHelpers.GetDivisor(engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth, period.Year, period.Month);
        int workingDaysInMonth = PayScheduleHelpers.GetPayableDaysInMonth(workWeek, period.Year, period.Month);

        // Load PT and LWF slabs for all employee work location states
        var stateCodes = activeEmployees
            .Select(e => workLocationStateMap.TryGetValue(e.WorkLocationId, out string? s) ? s : null)
            .Where(s => s is not null)
            .Select(s => s!)
            .Distinct()
            .ToList();

        var ptSlabs = new List<ProfessionalTaxSlab>();
        foreach (var state in stateCodes)
        {
            var slabs = await statutoryRepo.GetPtSlabsAsync(state, new DateOnly(period.Year, period.Month, 1), ct);
            ptSlabs.AddRange(slabs);
        }

        var lwfConfigs = stateCodes.Count > 0
            ? await statutoryRepo.GetLwfConfigsAsync(stateCodes, ct)
            : [];

        var staticConfig = StatutoryConfigBuilder.Build(orgConfig, taxConfig, taxSlabs, surchargeSlabs, ptSlabs, lwfConfigs);
        string snapshot = JsonSerializer.Serialize(staticConfig);

        // Load prior employer YTD for mid-year joiners (bulk, keyed by employeeId)
        IReadOnlyList<Guid> employeeIds = activeEmployees.Select(e => e.Id).ToList();
        IReadOnlyList<Domain.Entities.PriorEmployerYtd> priorYtdList =
            await priorYtdRepo.GetByEmployeesAndFiscalYearAsync(employeeIds, period.FiscalYear, ct);
        Dictionary<Guid, Domain.Entities.PriorEmployerYtd> priorYtdByEmployee =
            priorYtdList.ToDictionary(p => p.EmployeeId);

        // Load current-employer YTD from approved/paid runs this FY in this system
        Dictionary<Guid, (decimal YtdGross, decimal YtdTds)> currentYtdByEmployee =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync(employeeIds, period.FiscalYear, ct);

        // Merge opening balances (pre-system months, same employer) into current-employer YTD
        IReadOnlyList<EmployeeFyOpening> openings =
            await fyOpeningRepo.GetByEmployeesAndFiscalYearAsync(employeeIds, period.FiscalYear, ct);
        foreach (EmployeeFyOpening opening in openings)
        {
            currentYtdByEmployee.TryGetValue(opening.EmployeeId, out var existing);
            currentYtdByEmployee[opening.EmployeeId] = (
                existing.YtdGross + opening.GrossSalary,
                existing.YtdTds + opening.TdsDeducted);
        }

        // Build engine inputs per employee
        var engineInputs = new List<EmployeeInput>();
        var eligibleMap = new Dictionary<Guid, (EmployeeSalaryStructure structure, SalaryStructureTemplate? template, string? skipReason)>();

        // Collect override-only component IDs for batch load
        var addedComponentIds = new HashSet<Guid>();
        var structureOverrideMap = new Dictionary<Guid, EmployeeSalaryStructure>();

        foreach (var emp in activeEmployees)
        {
            var salaryStructure = await salaryStructureRepo.GetActiveWithOverridesAsync(emp.Id, ct);
            if (salaryStructure is null)
            {
                eligibleMap[emp.Id] = (null!, null, "No active salary structure");
                continue;
            }

            structureOverrideMap[emp.Id] = salaryStructure;

            SalaryStructureTemplate? template = salaryStructure.SalaryStructureTemplateId.HasValue
                ? await templateRepo.GetByIdWithComponentsAsync(salaryStructure.SalaryStructureTemplateId.Value, ct)
                : null;

            // Identify override-only components (not in template)
            if (salaryStructure.ComponentOverrides.Count > 0 && template is not null)
            {
                var templateCompIds = new HashSet<Guid>(template.Components.Select(c => c.ComponentId));
                foreach (var ov in salaryStructure.ComponentOverrides)
                {
                    if (!templateCompIds.Contains(ov.SalaryComponentId))
                        addedComponentIds.Add(ov.SalaryComponentId);
                }
            }

            // Hard-block onboarding checks
            string? skipReason = null;
            if (emp.DateOfBirth == default) skipReason = "Onboarding incomplete: Date of Birth missing";
            else if (string.IsNullOrWhiteSpace(emp.FathersName)) skipReason = "Onboarding incomplete: Father's Name missing";
            else if (string.IsNullOrWhiteSpace(emp.EncryptedBankAccount)) skipReason = "Onboarding incomplete: Bank account missing";

            eligibleMap[emp.Id] = (salaryStructure, template, skipReason);
        }

        // Batch-load SalaryComponent details for added (override-only) components
        Dictionary<Guid, SalaryComponent> addedCompDetails = addedComponentIds.Count > 0
            ? (await salaryComponentRepo.GetByIdsAsync([.. addedComponentIds], ct)).ToDictionary(c => c.Id)
            : [];

        foreach (var emp in activeEmployees)
        {
            if (!eligibleMap.TryGetValue(emp.Id, out var entry)) continue;
            var (salaryStructure, template, skipReason) = entry;
            if (salaryStructure is null) continue;

            if (skipReason is null)
            {
                var components = BuildComponentInputs(salaryStructure, template, addedCompDetails, staticConfig);
                decimal basicWage = components.FirstOrDefault(c => c.Code == "BASICSALARY")?.Amount ?? 0m;
                bool hasPan = !string.IsNullOrWhiteSpace(emp.EncryptedPAN);
                string workState = workLocationStateMap.TryGetValue(emp.WorkLocationId, out string? wls) ? wls : "MH";
                var (hyIndex, hyTotal) = period.HalfYearPosition(emp.DateOfJoining);
                currentYtdByEmployee.TryGetValue(emp.Id, out var curYtd);
                engineInputs.Add(new EmployeeInput(
                    EmployeeId: emp.Id,
                    EmployeeCode: emp.EmployeeCode,
                    WorkStateCode: workState,
                    EpfEnabled: emp.EpfEnabled,
                    IsESIExempt: !emp.EsiEnabled,
                    IsPWD: emp.IsPWD,
                    MonthlyCTC: salaryStructure.AnnualCTC / 12m,
                    Components: components,
                    LOPDays: 0,
                    WorkingDaysInMonth: workingDaysInMonth,
                    VPFAmount: 0,
                    PriorEmployerYTDTaxableIncome: priorYtdByEmployee.TryGetValue(emp.Id, out var ytd) ? ytd.GrossSalary : 0m,
                    PriorEmployerYTDTDSDeducted: ytd?.TdsDeducted ?? 0m,
                    PriorEmployerYTDPF: 0m,
                    HalfYearMonthIndex: hyIndex,
                    HalfYearTotalMonths: hyTotal,
                    BasicWage: basicWage,
                    HasPan: hasPan,
                    CurrentEmployerYTDGross: curYtd.YtdGross,
                    CurrentEmployerYTDTDSDeducted: curYtd.YtdTds));
            }
        }

        var runInput = new PayrollRunInput(
            Year: period.Year,
            Month: period.Month,
            CalendarDaysInMonth: calendarDays,
            SalaryDivisor: salaryDivisor,
            MonthsRemainingInFY: period.MonthsRemainingInFiscalYear(),
            FiscalYearLabel: period.FiscalYearLabel);

        // Run engine for eligible employees
        var results = engineInputs.Count > 0
            ? PayrollEngine.Compute(engineInputs, runInput, staticConfig)
            : [];

        var resultMap = results.ToDictionary(r => r.EmployeeId);
        var epfFlagByComponent = engineInputs
            .SelectMany(e => e.Components)
            .GroupBy(c => c.ComponentId)
            .ToDictionary(g => g.Key, g => g.First().ConsiderForEpf);
        var showInPayslipByComponent = engineInputs
            .SelectMany(e => e.Components)
            .GroupBy(c => c.ComponentId)
            .ToDictionary(g => g.Key, g => g.First().ShowInPayslip);

        // Create PayrollRun
        int employeeCount = activeEmployees.Count;
        var payrollRun = PayrollRun.Create(
            tenantId: tenantContext.TenantId,
            payPeriod: period,
            type: PayrollRunType.Regular,
            payDay: payDay,
            statutoryConfigSnapshot: snapshot,
            employeeCount: employeeCount,
            createdBy: req.ActorId);

        await payrollRunRepo.AddAsync(payrollRun, ct);

        // Create PayrunEmployee + PayrunComponentBreakdown rows
        var createdPayrunEmployees = new List<PayrunEmployee>();
        var tdsWorksheets = new List<TdsWorksheet>();

        foreach (var emp in activeEmployees)
        {
            if (!eligibleMap.TryGetValue(emp.Id, out var info)) continue;

            var payrunEmp = PayrunEmployee.Create(
                payrollRun.Id, emp.Id, tenantContext.TenantId, calendarDays, req.ActorId);

            if (info.skipReason is not null || info.structure is null)
            {
                payrunEmp.Skip(info.skipReason ?? "No active salary structure", req.ActorId);
            }
            else if (resultMap.TryGetValue(emp.Id, out var result))
            {
                payrunEmp.UpdateComputedAmounts(
                    grossPay: result.Gross.GrossWage,
                    netPay: result.NetPay,
                    taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
                    benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
                    reimbursementsAmount: 0m,
                    employeePf: result.PF.EmployeeContribution,
                    employerPf: result.PF.EPFEmployerContribution,
                    employeeEsi: result.ESI.EmployeeContribution,
                    employerEsi: result.ESI.EmployerContribution,
                    ptAmount: result.PT.Amount,
                    tdsAmount: result.TDS.MonthlyTDS,
                    lwfEmployeeAmount: result.LWF.EmployeeAmount,
                    lwfEmployerAmount: result.LWF.EmployerAmount,
                    gratuityAmount: result.Gratuity.MonthlyAccrual,
                    epsAmount: result.PF.EPSEmployerContribution,
                    monthlyCTC: info.structure.AnnualCTC / 12m,
                    actorId: req.ActorId);

                // Build TdsWorksheet for this employee
                priorYtdByEmployee.TryGetValue(emp.Id, out var empYtd);
                currentYtdByEmployee.TryGetValue(emp.Id, out var wsYtd);
                decimal ytdTdsDeducted = (empYtd?.TdsDeducted ?? 0m) + wsYtd.YtdTds;
                tdsWorksheets.Add(TdsWorksheet.Create(
                    payrollRunId: payrollRun.Id,
                    employeeId: emp.Id,
                    tenantId: tenantContext.TenantId,
                    fiscalYear: period.FiscalYear,
                    annualProjectedIncome: result.TDS.TaxableIncome + staticConfig.StandardDeduction,
                    standardDeduction: staticConfig.StandardDeduction,
                    taxableIncome: result.TDS.TaxableIncome,
                    taxBeforeRebate: result.TDS.TaxBeforeRebate,
                    rebate87A: result.TDS.Rebate87AApplied ? Math.Min(result.TDS.TaxBeforeRebate, staticConfig.Rebate87AAmount) : 0m,
                    surcharge: result.TDS.Surcharge,
                    cess: result.TDS.Cess,
                    annualTaxLiability: result.TDS.AnnualProjectedTax,
                    ytdTdsDeducted: ytdTdsDeducted,
                    remainingMonthsInFy: runInput.MonthsRemainingInFY,
                    tdsThisMonth: result.TDS.MonthlyTDS,
                    hasPanOverride: result.TDS.HasPanOverride,
                    createdBy: req.ActorId));

                // Component breakdowns
                foreach (var comp in result.Gross.ComponentBreakdown)
                {
                    var breakdown = PayrunComponentBreakdown.Create(
                        payrollRun.Id, emp.Id, tenantContext.TenantId,
                        comp.ComponentId, comp.Code, comp.Code,
                        comp.FullAmount, comp.ProratedAmount,
                        isOneTimeEarning: false,
                        considerForEpf: epfFlagByComponent.GetValueOrDefault(comp.ComponentId, false),
                        showInPayslip: showInPayslipByComponent.GetValueOrDefault(comp.ComponentId, true));
                    await breakdownRepo.AddAsync(breakdown, ct);
                }
            }

            await payrunEmployeeRepo.AddAsync(payrunEmp, ct);
            createdPayrunEmployees.Add(payrunEmp);
        }

        // Run-level financial summary computed via shared calculator so initiation,
        // LOP, one-time entries, and reimbursement imports all use the same formula.
        var activePayrunEmployees = createdPayrunEmployees
            .Where(p => p.Status == PayrunEmployeeStatus.Active)
            .ToList();
        PayrollCostSnapshot snapshot2 = costCalculator.Calculate(activePayrunEmployees);
        payrollRun.UpdateFinancialSummary(
            snapshot2.PayrollCost,
            snapshot2.TotalNet,
            snapshot2.TotalEmployerPf,
            snapshot2.TotalEmployerEsi,
            snapshot2.TotalTds,
            snapshot2.TotalPt,
            employeeCount,
            req.ActorId);

        if (tdsWorksheets.Count > 0)
            await tdsWorksheetRepo.AddRangeAsync(tdsWorksheets, ct);

        await uow.SaveChangesAsync(ct);

        return new PayrollRunSummaryDto(
            Id: payrollRun.Id,
            Year: period.Year,
            Month: period.Month,
            PeriodLabel: period.ToString(),
            Status: payrollRun.Status.ToString(),
            Type: payrollRun.Type.ToString(),
            PayDay: payDay,
            PayrollCost: snapshot2.PayrollCost,
            TotalNetPay: snapshot2.TotalNet,
            TotalEmployerPf: snapshot2.TotalEmployerPf,
            TotalEmployerEsi: snapshot2.TotalEmployerEsi,
            TotalTds: snapshot2.TotalTds,
            TotalPt: snapshot2.TotalPt,
            EmployeeCount: employeeCount,
            CreatedAt: payrollRun.CreatedAt,
            ApprovedAt: payrollRun.ApprovedAt,
            PaidAt: payrollRun.PaidAt);
    }

    internal static IReadOnlyList<SalaryComponentInput> BuildComponentInputs(
        EmployeeSalaryStructure structure,
        SalaryStructureTemplate? template,
        Dictionary<Guid, SalaryComponent> addedCompDetails,
        StatutoryConfig config)
    {
        if (template is null) return [];

        Dictionary<Guid, EmployeeSalaryComponentOverride> overrideMap =
            structure.ComponentOverrides.ToDictionary(o => o.SalaryComponentId);

        decimal monthlyCTC = structure.AnnualCTC / 12m;
        var raw = new List<(Guid Id, string Code, decimal Amount, bool IsTaxable, bool ConsiderForEpf, EpfInclusionRule EpfRule, bool ConsiderForEsi, bool CalculateOnProRata, bool IsFlat, bool ShowInPayslip)>();
        decimal basicMonthly = 0m;
        decimal nonResidualSum = 0m;

        var ordered = template.Components.OrderBy(c => c.DisplayOrder).ToList();
        var templateCompIds = new HashSet<Guid>(ordered.Select(c => c.ComponentId));

        // Pass 1: non-residual, non-PercentOfGross components (CTC/Basic/Fixed — no Gross dependency)
        foreach (SalaryStructureComponent comp in ordered)
        {
            if (comp.Component is null) continue;
            if (comp.FormulaType == ComponentFormulaType.ResidualCTC) continue;

            overrideMap.TryGetValue(comp.ComponentId, out EmployeeSalaryComponentOverride? ov);
            ComponentFormulaType effectiveType = ov?.FormulaType ?? comp.FormulaType;
            if (effectiveType == ComponentFormulaType.PercentOfGross) continue; // deferred to Pass 2

            decimal? effectivePct = ov?.Percentage ?? comp.Percentage;
            decimal? effectiveFixed = ov?.FixedAmount ?? comp.FixedAmount;

            decimal monthly = effectiveType switch
            {
                ComponentFormulaType.PercentOfCTC =>
                    Math.Round(structure.AnnualCTC * (effectivePct!.Value / 100m) / 12m, 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfBasic =>
                    Math.Round(basicMonthly * (effectivePct!.Value / 100m), 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.Fixed =>
                    effectiveFixed ?? 0m,
                _ => 0m
            };

            if (comp.Component.EarningType == EarningType.Basic) basicMonthly = monthly;
            nonResidualSum += monthly;

            raw.Add((comp.ComponentId, comp.Component.Code, monthly,
                comp.Component.IsTaxable ?? true,
                comp.Component.ConsiderForEpf ?? false,
                comp.Component.EpfInclusionRule ?? EpfInclusionRule.Always,
                comp.Component.ConsiderForEsi ?? false,
                comp.Component.CalculateOnProRata ?? true,
                comp.Component.PayType == PayType.FlatAmount,
                comp.Component.ShowInPayslip ?? true));
        }

        // Compute employer contributions included in CTC to derive actual Gross
        // PF wage uses only Pass-1 components (no residual, no PercentOfGross) — avoids circular dependency
        decimal alwaysPfWagePass1 = raw
            .Where(r => r.ConsiderForEpf && r.EpfRule == EpfInclusionRule.Always)
            .Sum(r => r.Amount);
        decimal pfWageForCtc = config.EpfRestrictEmployerWage
            ? Math.Min(alwaysPfWagePass1, config.PFWageCap)
            : alwaysPfWagePass1;

        decimal employerCtcDeductions = 0m;

        if (config.PFEnabled && config.EpfIncludeEmployerInCtc)
        {
            decimal epsWage = Math.Min(pfWageForCtc, config.PFWageCap);
            decimal eps = Math.Min(
                Math.Round(epsWage * config.EPSEmployerRate, 2, MidpointRounding.AwayFromZero),
                config.EPSCap);
            decimal epfEmployer = Math.Round(pfWageForCtc * config.EPFEmployeeRate, 2, MidpointRounding.AwayFromZero) - eps;
            employerCtcDeductions += eps + epfEmployer;
        }
        if (config.GratuityIncludedInCtc && basicMonthly > 0m)
            employerCtcDeductions += Math.Round(basicMonthly * 15m / 26m / 12m, 2, MidpointRounding.AwayFromZero);

        decimal monthlyGross = Math.Max(0m, monthlyCTC - employerCtcDeductions);

        // Pass 2: PercentOfGross components (now Gross is known)
        foreach (SalaryStructureComponent comp in ordered)
        {
            if (comp.Component is null) continue;
            if (comp.FormulaType == ComponentFormulaType.ResidualCTC) continue;

            overrideMap.TryGetValue(comp.ComponentId, out EmployeeSalaryComponentOverride? ov);
            ComponentFormulaType effectiveType = ov?.FormulaType ?? comp.FormulaType;
            if (effectiveType != ComponentFormulaType.PercentOfGross) continue;

            decimal? effectivePct = ov?.Percentage ?? comp.Percentage;
            decimal monthly = Math.Round(monthlyGross * (effectivePct!.Value / 100m), 2, MidpointRounding.AwayFromZero);
            nonResidualSum += monthly;

            raw.Add((comp.ComponentId, comp.Component.Code, monthly,
                comp.Component.IsTaxable ?? true,
                comp.Component.ConsiderForEpf ?? false,
                comp.Component.EpfInclusionRule ?? EpfInclusionRule.Always,
                comp.Component.ConsiderForEsi ?? false,
                comp.Component.CalculateOnProRata ?? true,
                comp.Component.PayType == PayType.FlatAmount,
                comp.Component.ShowInPayslip ?? true));
        }

        // Residual: Special Allowance = Gross − all other components
        SalaryStructureComponent? residual = ordered.FirstOrDefault(c => c.FormulaType == ComponentFormulaType.ResidualCTC);
        if (residual?.Component is not null)
        {
            decimal residualMonthly = Math.Round(monthlyGross - nonResidualSum, 2, MidpointRounding.AwayFromZero);
            raw.Add((residual.ComponentId, residual.Component.Code, residualMonthly,
                residual.Component.IsTaxable ?? true,
                residual.Component.ConsiderForEpf ?? false,
                residual.Component.EpfInclusionRule ?? EpfInclusionRule.Always,
                residual.Component.ConsiderForEsi ?? false,
                residual.Component.CalculateOnProRata ?? true,
                residual.Component.PayType == PayType.FlatAmount,
                residual.Component.ShowInPayslip ?? true));
        }

        // Pass 3: override-only components (not in template)
        foreach (EmployeeSalaryComponentOverride ov in structure.ComponentOverrides)
        {
            if (templateCompIds.Contains(ov.SalaryComponentId)) continue;
            if (!addedCompDetails.TryGetValue(ov.SalaryComponentId, out SalaryComponent? sc)) continue;

            decimal monthly = ov.FormulaType switch
            {
                ComponentFormulaType.Fixed =>
                    ov.FixedAmount ?? 0m,
                ComponentFormulaType.PercentOfCTC =>
                    Math.Round(structure.AnnualCTC * (ov.Percentage ?? 0m) / 100m / 12m, 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfBasic =>
                    Math.Round(basicMonthly * (ov.Percentage ?? 0m) / 100m, 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfGross =>
                    Math.Round(monthlyGross * (ov.Percentage ?? 0m) / 100m, 2, MidpointRounding.AwayFromZero),
                _ => 0m
            };
            raw.Add((ov.SalaryComponentId, sc.Code, monthly,
                sc.IsTaxable ?? true,
                sc.ConsiderForEpf ?? false,
                sc.EpfInclusionRule ?? EpfInclusionRule.Always,
                sc.ConsiderForEsi ?? false,
                sc.CalculateOnProRata ?? true,
                sc.PayType == PayType.FlatAmount,
                sc.ShowInPayslip ?? true));
        }

        // Resolve OnlyWhenPfWageBelowLimit using final PF wage (includes all passes)
        decimal alwaysPfWage = raw
            .Where(r => r.ConsiderForEpf && r.EpfRule == EpfInclusionRule.Always)
            .Sum(r => r.Amount);

        return raw
            .Select(r =>
            {
                bool considerForEpf = r.EpfRule == EpfInclusionRule.OnlyWhenPfWageBelowLimit
                    ? r.ConsiderForEpf && alwaysPfWage < config.PFWageCap
                    : r.ConsiderForEpf;
                return new SalaryComponentInput(r.Id, r.Code, r.Amount, r.IsTaxable, considerForEpf,
                    r.ConsiderForEsi, r.CalculateOnProRata, r.IsFlat, r.ShowInPayslip);
            })
            .ToList();
    }
}
