using FluentValidation;
using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
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
    IStatutoryConfigRepository statutoryRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IWorkLocationRepository workLocationRepo,
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

        string fiscalYear = period.FiscalYearLabel.Replace("FY", "");
        var taxConfig = await statutoryRepo.GetIncomeTaxConfigAsync(fiscalYear, "New", ct);
        var taxSlabs = await statutoryRepo.GetIncomeTaxSlabsAsync(fiscalYear, "New", ct);
        var surchargeSlabs = await statutoryRepo.GetSurchargeSlabsAsync(fiscalYear, "New", ct);

        var employees = await employeeRepo.ListAsync(ct);
        var activeEmployees = employees.Where(e => e.Status == EmployeeStatus.Active).ToList();

        var workLocations = await workLocationRepo.ListAsync(ct);
        var workLocationStateMap = workLocations.ToDictionary(wl => wl.Id, wl => wl.State.ToString());

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

        // Build engine inputs per employee
        var engineInputs = new List<EmployeeInput>();
        var eligibleMap = new Dictionary<Guid, (EmployeeSalaryStructure structure, SalaryStructureTemplate? template, string? skipReason)>();

        foreach (var emp in activeEmployees)
        {
            var salaryStructure = await salaryStructureRepo.GetActiveAsync(emp.Id, ct);
            if (salaryStructure is null)
            {
                eligibleMap[emp.Id] = (null!, null, "No active salary structure");
                continue;
            }

            SalaryStructureTemplate? template = salaryStructure.SalaryStructureTemplateId.HasValue
                ? await templateRepo.GetByIdWithComponentsAsync(salaryStructure.SalaryStructureTemplateId.Value, ct)
                : null;

            // Hard-block onboarding checks
            string? skipReason = null;
            if (emp.DateOfBirth == default) skipReason = "Onboarding incomplete: Date of Birth missing";
            else if (string.IsNullOrWhiteSpace(emp.FathersName)) skipReason = "Onboarding incomplete: Father's Name missing";
            else if (string.IsNullOrWhiteSpace(emp.EncryptedBankAccount)) skipReason = "Onboarding incomplete: Bank account missing";

            eligibleMap[emp.Id] = (salaryStructure, template, skipReason);

            if (skipReason is null)
            {
                var components = BuildComponentInputs(salaryStructure, template);
                bool hasPan = !string.IsNullOrWhiteSpace(emp.EncryptedPAN);
                string workState = workLocationStateMap.TryGetValue(emp.WorkLocationId, out string? wls) ? wls : "MH";
                var (hyIndex, hyTotal) = period.HalfYearPosition(emp.DateOfJoining);

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
                    PriorEmployerYTDTaxableIncome: 0,
                    PriorEmployerYTDTDSDeducted: 0,
                    PriorEmployerYTDPF: 0,
                    HalfYearMonthIndex: hyIndex,
                    HalfYearTotalMonths: hyTotal));
            }
        }

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
            var slabs = await statutoryRepo.GetPtSlabsAsync(state, DateOnly.FromDateTime(DateTime.UtcNow), ct);
            ptSlabs.AddRange(slabs);
        }

        var lwfConfigs = stateCodes.Count > 0
            ? await statutoryRepo.GetLwfConfigsAsync(stateCodes, ct)
            : [];

        var staticConfig = StatutoryConfigBuilder.Build(orgConfig, taxConfig, taxSlabs, surchargeSlabs, ptSlabs, lwfConfigs);

        // Serialize snapshot for reproducibility
        string snapshot = JsonSerializer.Serialize(staticConfig);

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
        decimal totalNetPay = 0m, totalEmployerPf = 0m, totalEmployerEsi = 0m, totalEdli = 0m, totalTds = 0m, totalPt = 0m;

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
                    edliAmount: result.PF.EDLIEmployerContribution,
                    actorId: req.ActorId);

                totalNetPay += result.NetPay;
                totalEmployerPf += result.PF.EPFEmployerContribution;
                totalEmployerEsi += result.ESI.EmployerContribution;
                totalEdli += result.PF.EDLIEmployerContribution;
                totalTds += result.TDS.MonthlyTDS;
                totalPt += result.PT.Amount;

                // Component breakdowns
                foreach (var comp in result.Gross.ComponentBreakdown)
                {
                    var breakdown = PayrunComponentBreakdown.Create(
                        payrollRun.Id, emp.Id, tenantContext.TenantId,
                        comp.ComponentId, comp.Code, comp.Code,
                        comp.FullAmount, comp.ProratedAmount,
                        isOneTimeEarning: false,
                        considerForEpf: epfFlagByComponent.GetValueOrDefault(comp.ComponentId, false));
                    await breakdownRepo.AddAsync(breakdown, ct);
                }
            }

            await payrunEmployeeRepo.AddAsync(payrunEmp, ct);
        }

        // Update payroll run financial summary
        decimal payrollCost = totalNetPay + totalEmployerPf + totalEmployerEsi + totalEdli;
        payrollRun.UpdateFinancialSummary(
            payrollCost, totalNetPay, totalEmployerPf, totalEmployerEsi, totalEdli, totalTds, totalPt,
            employeeCount, req.ActorId);

        await uow.SaveChangesAsync(ct);

        return new PayrollRunSummaryDto(
            Id: payrollRun.Id,
            Year: period.Year,
            Month: period.Month,
            PeriodLabel: period.ToString(),
            Status: payrollRun.Status.ToString(),
            Type: payrollRun.Type.ToString(),
            PayDay: payDay,
            PayrollCost: payrollCost,
            TotalNetPay: totalNetPay,
            TotalEmployerPf: totalEmployerPf,
            TotalEmployerEsi: totalEmployerEsi,
            TotalTds: totalTds,
            TotalPt: totalPt,
            EmployeeCount: employeeCount,
            CreatedAt: payrollRun.CreatedAt,
            ApprovedAt: payrollRun.ApprovedAt,
            PaidAt: payrollRun.PaidAt);
    }

    private static IReadOnlyList<SalaryComponentInput> BuildComponentInputs(
        EmployeeSalaryStructure structure,
        SalaryStructureTemplate? template)
    {
        if (template is null) return [];

        decimal monthlyGross = structure.AnnualCTC / 12m;
        var components = new List<SalaryComponentInput>();
        decimal basicMonthly = 0m;
        decimal nonResidualSum = 0m;

        var ordered = template.Components.OrderBy(c => c.DisplayOrder).ToList();

        foreach (var comp in ordered)
        {
            if (comp.Component is null) continue;
            if (comp.FormulaType == ComponentFormulaType.ResidualCTC) continue;

            decimal monthly = comp.FormulaType switch
            {
                ComponentFormulaType.PercentOfCTC =>
                    Math.Round(structure.AnnualCTC * (comp.Percentage!.Value / 100m) / 12m, 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfBasic =>
                    Math.Round(basicMonthly * (comp.Percentage!.Value / 100m), 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.PercentOfGross =>
                    Math.Round(monthlyGross * (comp.Percentage!.Value / 100m), 2, MidpointRounding.AwayFromZero),
                ComponentFormulaType.Fixed =>
                    comp.FixedAmount ?? 0m,
                _ => 0m
            };

            if (comp.Component.Code == "BASIC") basicMonthly = monthly;
            nonResidualSum += monthly;

            bool isTaxable = comp.Component.IsTaxable ?? true;
            bool considerForEpf = comp.Component.ConsiderForEpf ?? false;
            components.Add(new SalaryComponentInput(comp.ComponentId, comp.Component.Code, monthly, isTaxable, considerForEpf));
        }

        // Residual
        var residual = ordered.FirstOrDefault(c => c.FormulaType == ComponentFormulaType.ResidualCTC);
        if (residual?.Component is not null)
        {
            decimal residualMonthly = Math.Round(monthlyGross - nonResidualSum, 2, MidpointRounding.AwayFromZero);
            bool isTaxable = residual.Component.IsTaxable ?? true;
            bool considerForEpf = residual.Component.ConsiderForEpf ?? false;
            components.Add(new SalaryComponentInput(residual.ComponentId, residual.Component.Code, residualMonthly, isTaxable, considerForEpf));
        }

        return components;
    }
}
