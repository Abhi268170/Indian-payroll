using FluentValidation;
using MediatR;
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

public record ReEvaluateSkippedCommand(Guid PayrollRunId, Guid ActorId) : IRequest<int>;

internal sealed class ReEvaluateSkippedValidator : AbstractValidator<ReEvaluateSkippedCommand>
{
    public ReEvaluateSkippedValidator()
    {
        RuleFor(x => x.PayrollRunId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class ReEvaluateSkippedHandler(
    IPayrollRunRepository payrollRunRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository salaryComponentRepo,
    IPayScheduleRepository payScheduleRepo,
    IWorkLocationRepository workLocationRepo,
    IPriorEmployerYtdRepository priorYtdRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IUnitOfWork uow)
    : IRequestHandler<ReEvaluateSkippedCommand, int>
{
    public async Task<int> Handle(ReEvaluateSkippedCommand req, CancellationToken ct)
    {
        PayrollRun payrollRun = await payrollRunRepo.GetByIdAsync(req.PayrollRunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.PayrollRunId} not found.");

        if (payrollRun.Status != PayrollRunStatus.Draft)
            throw new DomainException("Only Draft payroll runs can be re-evaluated.");

        if (string.IsNullOrEmpty(payrollRun.StatutoryConfigSnapshot))
            throw new DomainException("Payroll run has no statutory config snapshot.");

        StatutoryConfig staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(payrollRun.StatutoryConfigSnapshot)
            ?? throw new DomainException("Failed to deserialize statutory config snapshot.");

        PayPeriod period = payrollRun.PayPeriod;

        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay schedule not configured.");

        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;

        int calendarDays = DateTime.DaysInMonth(period.Year, period.Month);
        int salaryDivisor = PayScheduleHelpers.GetDivisor(engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth, period.Year, period.Month);
        int workingDaysInMonth = PayScheduleHelpers.GetPayableDaysInMonth(workWeek, period.Year, period.Month);

        var workLocations = await workLocationRepo.ListAsync(ct);
        Dictionary<Guid, string> workLocationStateMap = workLocations.ToDictionary(wl => wl.Id, wl => wl.State.ToIsoCode());

        IReadOnlyList<PayrunEmployee> skipped = await payrunEmployeeRepo.GetByRunIdWithStatusAsync(
            req.PayrollRunId, PayrunEmployeeStatus.Skipped, ct);

        List<PayrunEmployee> onboardingBlocked = skipped
            .Where(pe => pe.SkipReason?.StartsWith("Onboarding incomplete", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        if (onboardingBlocked.Count == 0) return 0;

        IReadOnlyList<Employee> allEmployees = await employeeRepo.ListAsync(ct);
        HashSet<Guid> targetIds = [.. onboardingBlocked.Select(pe => pe.EmployeeId)];
        Dictionary<Guid, Employee> empById = allEmployees
            .Where(e => targetIds.Contains(e.Id))
            .ToDictionary(e => e.Id);

        IReadOnlyList<PriorEmployerYtd> priorYtdList =
            await priorYtdRepo.GetByEmployeesAndFiscalYearAsync([.. targetIds], period.FiscalYear, ct);
        Dictionary<Guid, PriorEmployerYtd> priorYtdByEmployee = priorYtdList.ToDictionary(p => p.EmployeeId);

        Dictionary<Guid, (decimal YtdGross, decimal YtdTds)> currentYtdByEmployee =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync([.. targetIds], period.FiscalYear, ct);

        IReadOnlyList<EmployeeFyOpening> openings =
            await fyOpeningRepo.GetByEmployeesAndFiscalYearAsync(targetIds, period.FiscalYear, ct);
        foreach (EmployeeFyOpening opening in openings)
        {
            currentYtdByEmployee.TryGetValue(opening.EmployeeId, out var existing);
            currentYtdByEmployee[opening.EmployeeId] = (
                existing.YtdGross + opening.GrossSalary,
                existing.YtdTds + opening.TdsDeducted);
        }

        var addedComponentIds = new HashSet<Guid>();
        var processedMap = new Dictionary<Guid, (PayrunEmployee PayrunEmp, EmployeeSalaryStructure Structure, SalaryStructureTemplate? Template)>();

        foreach (PayrunEmployee payrunEmp in onboardingBlocked)
        {
            if (!empById.TryGetValue(payrunEmp.EmployeeId, out Employee? emp)) continue;

            string? newSkipReason = null;
            if (emp.DateOfBirth == default) newSkipReason = "Onboarding incomplete: Date of Birth missing";
            else if (string.IsNullOrWhiteSpace(emp.FathersName)) newSkipReason = "Onboarding incomplete: Father's Name missing";
            else if (string.IsNullOrWhiteSpace(emp.EncryptedBankAccount)) newSkipReason = "Onboarding incomplete: Bank account missing";

            if (newSkipReason is not null)
            {
                if (newSkipReason != payrunEmp.SkipReason)
                {
                    payrunEmp.UndoSkip(req.ActorId);
                    payrunEmp.Skip(newSkipReason, req.ActorId);
                    payrunEmployeeRepo.Update(payrunEmp);
                }
                continue;
            }

            EmployeeSalaryStructure? salaryStructure = await salaryStructureRepo.GetActiveWithOverridesAsync(emp.Id, ct);
            if (salaryStructure is null)
            {
                payrunEmp.UndoSkip(req.ActorId);
                payrunEmp.Skip("No active salary structure", req.ActorId);
                payrunEmployeeRepo.Update(payrunEmp);
                continue;
            }

            SalaryStructureTemplate? template = salaryStructure.SalaryStructureTemplateId.HasValue
                ? await templateRepo.GetByIdWithComponentsAsync(salaryStructure.SalaryStructureTemplateId.Value, ct)
                : null;

            if (salaryStructure.ComponentOverrides.Count > 0 && template is not null)
            {
                HashSet<Guid> templateCompIds = [.. template.Components.Select(c => c.ComponentId)];
                foreach (EmployeeSalaryComponentOverride ov in salaryStructure.ComponentOverrides)
                {
                    if (!templateCompIds.Contains(ov.SalaryComponentId))
                        addedComponentIds.Add(ov.SalaryComponentId);
                }
            }

            processedMap[emp.Id] = (payrunEmp, salaryStructure, template);
        }

        if (processedMap.Count == 0)
        {
            await uow.SaveChangesAsync(ct);
            return 0;
        }

        Dictionary<Guid, SalaryComponent> addedCompDetails = addedComponentIds.Count > 0
            ? (await salaryComponentRepo.GetByIdsAsync([.. addedComponentIds], ct)).ToDictionary(c => c.Id)
            : [];

        var engineInputs = new List<EmployeeInput>();
        foreach ((Guid empId, (PayrunEmployee _, EmployeeSalaryStructure salaryStructure, SalaryStructureTemplate? template)) in processedMap)
        {
            if (!empById.TryGetValue(empId, out Employee? emp)) continue;

            IReadOnlyList<SalaryComponentInput> components = InitiatePayrollRunHandler.BuildComponentInputs(
                salaryStructure, template, addedCompDetails, staticConfig);
            decimal basicWage = components.FirstOrDefault(c => c.Code == "BASICSALARY")?.Amount ?? 0m;
            bool hasPan = !string.IsNullOrWhiteSpace(emp.EncryptedPAN);
            string workState = workLocationStateMap.TryGetValue(emp.WorkLocationId, out string? wls) ? wls : "MH";
            (int hyIndex, int hyTotal) = period.HalfYearPosition(emp.DateOfJoining);

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
                CurrentEmployerYTDGross: currentYtdByEmployee.TryGetValue(emp.Id, out var curYtd) ? curYtd.YtdGross : 0m,
                CurrentEmployerYTDTDSDeducted: curYtd.YtdTds));
        }

        var runInput = new PayrollRunInput(
            Year: period.Year,
            Month: period.Month,
            CalendarDaysInMonth: calendarDays,
            SalaryDivisor: salaryDivisor,
            MonthsRemainingInFY: period.MonthsRemainingInFiscalYear(),
            FiscalYearLabel: period.FiscalYearLabel);

        var results = PayrollEngine.Compute(engineInputs, runInput, staticConfig);
        var resultMap = results.ToDictionary(r => r.EmployeeId);

        Dictionary<Guid, bool> epfFlagByComponent = engineInputs
            .SelectMany(e => e.Components)
            .GroupBy(c => c.ComponentId)
            .ToDictionary(g => g.Key, g => g.First().ConsiderForEpf);

        decimal deltaGross = 0m, deltaNet = 0m, deltaEmployerPf = 0m, deltaEmployerEps = 0m;
        decimal deltaEmployerEsi = 0m, deltaTds = 0m;
        decimal deltaPt = 0m, deltaLwfEmployer = 0m, deltaGratuity = 0m;
        int recomputedCount = 0;

        foreach ((Guid empId, (PayrunEmployee payrunEmp, EmployeeSalaryStructure salaryStructure, SalaryStructureTemplate? _)) in processedMap)
        {
            if (!resultMap.TryGetValue(empId, out var result)) continue;

            payrunEmp.UndoSkip(req.ActorId);
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
                monthlyCTC: salaryStructure.AnnualCTC / 12m,
                actorId: req.ActorId);

            payrunEmployeeRepo.Update(payrunEmp);

            // Upsert TDS worksheet
            await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(req.PayrollRunId, empId, ct);
            priorYtdByEmployee.TryGetValue(empId, out var empYtd);
            await tdsWorksheetRepo.AddAsync(TdsWorksheet.Create(
                payrollRunId: req.PayrollRunId,
                employeeId: empId,
                tenantId: payrunEmp.TenantId,
                fiscalYear: period.FiscalYear,
                annualProjectedIncome: result.TDS.TaxableIncome + staticConfig.StandardDeduction,
                standardDeduction: staticConfig.StandardDeduction,
                taxableIncome: result.TDS.TaxableIncome,
                taxBeforeRebate: result.TDS.TaxBeforeRebate,
                rebate87A: result.TDS.Rebate87AApplied ? Math.Min(result.TDS.TaxBeforeRebate, staticConfig.Rebate87AAmount) : 0m,
                surcharge: result.TDS.Surcharge,
                cess: result.TDS.Cess,
                annualTaxLiability: result.TDS.AnnualProjectedTax,
                ytdTdsDeducted: empYtd?.TdsDeducted ?? 0m,
                remainingMonthsInFy: runInput.MonthsRemainingInFY,
                tdsThisMonth: result.TDS.MonthlyTDS,
                hasPanOverride: result.TDS.HasPanOverride,
                createdBy: req.ActorId), ct);

            await breakdownRepo.RemoveRangeByRunAndEmployeeAsync(req.PayrollRunId, empId, ct);
            foreach (var comp in result.Gross.ComponentBreakdown)
            {
                PayrunComponentBreakdown breakdown = PayrunComponentBreakdown.Create(
                    req.PayrollRunId, empId, payrunEmp.TenantId,
                    comp.ComponentId, comp.Code, comp.Code,
                    comp.FullAmount, comp.ProratedAmount,
                    isOneTimeEarning: false,
                    considerForEpf: epfFlagByComponent.GetValueOrDefault(comp.ComponentId, false));
                await breakdownRepo.AddAsync(breakdown, ct);
            }

            deltaGross += result.Gross.GrossWage;
            deltaNet += result.NetPay;
            deltaEmployerPf += result.PF.EPFEmployerContribution;
            deltaEmployerEps += result.PF.EPSEmployerContribution;
            deltaEmployerEsi += result.ESI.EmployerContribution;
            deltaTds += result.TDS.MonthlyTDS;
            deltaPt += result.PT.Amount;
            deltaLwfEmployer += result.LWF.EmployerAmount;
            deltaGratuity += result.Gratuity.MonthlyAccrual;
            recomputedCount++;
        }

        decimal newPayrollCost = payrollRun.PayrollCost
            + deltaGross
            + deltaEmployerPf + deltaEmployerEps
            + deltaEmployerEsi
            + deltaLwfEmployer
            + deltaGratuity;

        payrollRun.UpdateFinancialSummary(
            payrollCost: newPayrollCost,
            totalNetPay: payrollRun.TotalNetPay + deltaNet,
            totalEmployerPf: payrollRun.TotalEmployerPf + deltaEmployerPf,
            totalEmployerEsi: payrollRun.TotalEmployerEsi + deltaEmployerEsi,
            totalTds: payrollRun.TotalTds + deltaTds,
            totalPt: payrollRun.TotalPt + deltaPt,
            employeeCount: payrollRun.EmployeeCount,
            actorId: req.ActorId);

        payrollRunRepo.Update(payrollRun);
        await uow.SaveChangesAsync(ct);
        return recomputedCount;
    }
}
