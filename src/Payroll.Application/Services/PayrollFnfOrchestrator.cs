using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Extensions;
using Payroll.Domain.Interfaces;
using Payroll.Engine;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using System.Text.Json;

namespace Payroll.Application.Services;

// Orchestrates the FnF engine call for one employee in a FinalSettlement
// or BulkFinalSettlement run. Mirrors PayrollRecomputeService for the regular
// path but applies FnF-specific tweaks: MonthsRemainingInFY=1 (closes annual
// TDS in one month), GratuityEnabled=false (we inject gratuity as IsFlat
// earning components instead so the exempt/taxable split is honored), and
// LWF is skipped when already deducted earlier in the same half-year.
public interface IPayrollFnfOrchestrator
{
    Task<FnfEngineResult> ComputeAsync(Guid fnfRunId, Guid employeeId, CancellationToken ct = default);

    // WI-22: compute against an in-memory breakdown set instead of the persisted
    // rows — used by the preview endpoint to model what-if amounts without saving.
    Task<FnfEngineResult> ComputeAsync(
        Guid fnfRunId, Guid employeeId,
        IReadOnlyList<PayrunComponentBreakdown> breakdownOverride,
        CancellationToken ct = default);
}

public sealed record FnfEngineResult(
    PayrollResult Engine,
    decimal ReimbursementsAmount,
    decimal NetPayWithAdjustments,
    // Total TDS deducted so far this FY (prior-employer + current-employer YTD).
    // Returned so callers can write TdsWorksheet without re-deriving YTD.
    decimal YtdTdsDeducted,
    // Deserialized statutory config used for this computation.
    // Returned so callers can write TdsWorksheet without re-deserializing.
    StatutoryConfig StaticConfig,
    // Fiscal year of the LWD month — may differ from run.PayPeriod.FiscalYear
    // for BulkFnF runs where LWD is in a different month than the pay date.
    int LwdFiscalYear);

public sealed class PayrollFnfOrchestrator(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeExitRepository exitRepo,
    IWorkLocationRepository workLocationRepo,
    IPayScheduleRepository payScheduleRepo,
    IPriorEmployerYtdRepository priorYtdRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo)
    : IPayrollFnfOrchestrator
{
    public Task<FnfEngineResult> ComputeAsync(Guid fnfRunId, Guid employeeId, CancellationToken ct = default) =>
        ComputeCoreAsync(fnfRunId, employeeId, breakdownOverride: null, ct);

    public async Task<FnfEngineResult> ComputeAsync(
        Guid fnfRunId, Guid employeeId,
        IReadOnlyList<PayrunComponentBreakdown> breakdownOverride,
        CancellationToken ct = default)
        => await ComputeCoreAsync(fnfRunId, employeeId, breakdownOverride, ct);

    private async Task<FnfEngineResult> ComputeCoreAsync(
        Guid fnfRunId, Guid employeeId,
        IReadOnlyList<PayrunComponentBreakdown>? breakdownOverride,
        CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(fnfRunId, ct)
            ?? throw new Domain.Common.NotFoundException($"FnF run {fnfRunId} not found.");

        if (run.Type != PayrollRunType.FinalSettlement && run.Type != PayrollRunType.BulkFinalSettlement)
            throw new Domain.Common.DomainException("PayrollFnfOrchestrator only handles FnF runs.");

        PayrunEmployee payrunEmp = await payrunEmpRepo.GetByRunAndEmployeeAsync(fnfRunId, employeeId, ct)
            ?? throw new Domain.Common.NotFoundException($"Employee {employeeId} not in FnF run.");

        Employee employee = await employeeRepo.GetByIdAsync(employeeId, ct)
            ?? throw new Domain.Common.NotFoundException($"Employee {employeeId} not found.");

        EmployeeExit exit = await exitRepo.GetActiveByEmployeeAsync(employeeId, ct)
            ?? throw new Domain.Common.DomainException($"No active exit for employee {employeeId}.");

        // Persisted breakdowns (recurring + saved FnF one-time), unless the caller
        // supplied an in-memory set for a preview (WI-22).
        var breakdowns = breakdownOverride
            ?? await breakdownRepo.GetByRunAndEmployeeAsync(fnfRunId, employeeId, ct);

        WorkLocation? workLocation = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
        string workStateCode = workLocation?.State.ToIsoCode() ?? "MH";

        PaySchedule paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new Domain.Common.DomainException("Pay Schedule not configured.");
        EngineSalaryCalculationMethod calcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;

        // WI-07: all month/FY-derived values use the LWD month, not run.PayPeriod.
        // For BulkFinalSettlement, run.PayPeriod is the pay-date month which may
        // differ from the LWD month. The statutory snapshot was already built for
        // the LWD month in InitiateExitCommand — the engine must be consistent.
        Domain.ValueObjects.PayPeriod lwdPeriod = new(exit.LastWorkingDay.Year, exit.LastWorkingDay.Month);

        int salaryDivisor = PayScheduleHelpers.GetDivisor(
            calcMethod, paySchedule.FixedWorkingDaysPerMonth,
            lwdPeriod.Year, lwdPeriod.Month);

        // WI-08: if the employee joined in the same month as their LWD, proration
        // starts from DateOfJoining rather than the 1st of the month.
        // CalendarDaysInMonth stays the full month total so the engine divides
        // correctly: workedDays / calendarDays gives the partial-month ratio.
        DateOnly lwdMonthStart = new(lwdPeriod.Year, lwdPeriod.Month, 1);
        DateOnly effectivePeriodStart = employee.DateOfJoining > lwdMonthStart
            ? employee.DateOfJoining
            : lwdMonthStart;
        int workedDays = exit.LastWorkingDay.DayNumber - effectivePeriodStart.DayNumber + 1;
        int calendarDaysInLwdMonth = DateTime.DaysInMonth(lwdPeriod.Year, lwdPeriod.Month);

        if (run.StatutoryConfigSnapshot is null)
            throw new Domain.Common.DomainException("FnF run missing statutory config snapshot.");
        StatutoryConfig staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

        // Partition reimbursement vs everything else (same rule as Phase 011).
        // WI-17: IsBenefit rows are employer-borne (already netted out of CTC) and
        // must NOT flow into gross/net/PF/ESI — exclude them from engine inputs,
        // same as reimbursements. They persist for payslip display only.
        var reimbursementRows = breakdowns.Where(IsReimbursement).ToList();
        var engineRows = breakdowns.Where(b => !IsReimbursement(b) && !b.IsBenefit).ToList();
        decimal reimbursementsAmount = reimbursementRows.Sum(b => b.FullAmount);

        IReadOnlyList<SalaryComponentInput> components = engineRows
            .Select(MapBreakdownToEngineInput)
            .ToList();

        decimal basicWage = engineRows
            .FirstOrDefault(b => b.ComponentCode == "BASICSALARY")?.FullAmount ?? 0m;

        bool hasPan = !string.IsNullOrWhiteSpace(employee.EncryptedPAN);
        var (hyIndex, hyTotal) = lwdPeriod.HalfYearPosition(employee.DateOfJoining);
        (decimal ytdGross, decimal ytdTaxableGross, decimal ytdTds) = await LoadCurrentYtdAsync(employeeId, lwdPeriod.FiscalYear, ct);

        // WI-05: merge pre-system opening balances into current-employer YTD.
        IReadOnlyList<EmployeeFyOpening> openings = await fyOpeningRepo
            .GetByEmployeesAndFiscalYearAsync([employeeId], lwdPeriod.FiscalYear, ct);
        EmployeeFyOpening? opening = openings.FirstOrDefault();
        if (opening != null)
        {
            ytdGross += opening.GrossSalary;
            ytdTaxableGross += opening.GrossSalary;
            ytdTds += opening.TdsDeducted;
        }

        // FnF closes the FY for this employee. Prior-employer YTD must be included
        // for mid-year joiners so the final TDS sweep accounts for the full year's
        // taxable income, not just current-employer earnings.
        IReadOnlyList<PriorEmployerYtd> priorList = await priorYtdRepo
            .GetByEmployeesAndFiscalYearAsync([employeeId], lwdPeriod.FiscalYear, ct);
        PriorEmployerYtd? priorYtd = priorList.FirstOrDefault();
        decimal priorTaxable = PriorEmployerYtdMapper.TaxableIncomeFor(priorYtd);
        decimal priorTds = priorYtd?.TdsDeducted ?? 0m;

        bool lwfAlreadyDeducted = await IsLwfAlreadyDeductedThisHalfYearAsync(employeeId, lwdPeriod, ct);

        // Effective LOP = unworked days in the LWD month (pre-joining + post-LWD)
        // plus any operator-set LOP for absences within the worked period.
        // The engine's proration formula: prorated = fullAmount × (salaryDivisor - lopDays) / salaryDivisor.
        // lopFromExit = salaryDivisor - workedDays ensures correct partial-month ratio.
        int lopFromExit = salaryDivisor - workedDays;
        int effectiveLopDays = lopFromExit + payrunEmp.LopDays;

        var empInput = new EmployeeInput(
            EmployeeId: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            WorkStateCode: workStateCode,
            EpfEnabled: employee.EpfEnabled,
            IsESIExempt: !employee.EsiEnabled,
            IsPWD: employee.IsPWD,
            MonthlyCTC: payrunEmp.MonthlyCTC,
            Components: components,
            LOPDays: effectiveLopDays,
            WorkingDaysInMonth: workedDays,
            VPFAmount: 0m,
            PriorEmployerYTDTaxableIncome: priorTaxable,
            PriorEmployerYTDTDSDeducted: priorTds,
            PriorEmployerYTDPF: 0m,
            HalfYearMonthIndex: hyIndex,
            HalfYearTotalMonths: hyTotal,
            BasicWage: basicWage,
            GratuityEnabled: false, // gratuity goes in as an IsFlat earning component instead
            HasPan: hasPan,
            CurrentEmployerYTDGross: ytdGross,
            CurrentEmployerYTDTDSDeducted: ytdTds,
            CurrentEmployerYTDTaxable: ytdTaxableGross);

        var runInput = new PayrollRunInput(
            Year: lwdPeriod.Year,
            Month: lwdPeriod.Month,
            CalendarDaysInMonth: workedDays, // unused by engine; kept for audit/context
            SalaryDivisor: salaryDivisor,
            MonthsRemainingInFY: 1, // forces full-year TDS closure
            FiscalYearLabel: lwdPeriod.FiscalYearLabel);

        PayrollResult result = PayrollEngine.Compute([empInput], runInput, staticConfig)[0];

        if (lwfAlreadyDeducted)
            result = result with { LWF = new LWFResult(0m, 0m, IsExempt: true) };

        decimal ytdTdsDeducted = priorTds + ytdTds;

        return new FnfEngineResult(
            Engine: result,
            ReimbursementsAmount: reimbursementsAmount,
            NetPayWithAdjustments: result.NetPay + reimbursementsAmount,
            YtdTdsDeducted: ytdTdsDeducted,
            StaticConfig: staticConfig,
            LwdFiscalYear: lwdPeriod.FiscalYear);
    }

    private async Task<(decimal Gross, decimal TaxableGross, decimal Tds)> LoadCurrentYtdAsync(
        Guid employeeId, int fiscalYear, CancellationToken ct)
    {
        Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)> ytdMap =
            await payrunEmpRepo.GetCurrentEmployerYtdAsync([employeeId], fiscalYear, ct);
        ytdMap.TryGetValue(employeeId, out var ytd);
        return (ytd.YtdGross, ytd.YtdTaxableGross, ytd.YtdTds);
    }

    private async Task<bool> IsLwfAlreadyDeductedThisHalfYearAsync(
        Guid employeeId, Domain.ValueObjects.PayPeriod period, CancellationToken ct)
    {
        foreach ((int year, int first, int last) in LwfHalfYearLookback.GetRanges(period.Year, period.Month))
        {
            if (await payrunEmpRepo.HasLwfDeductedInPeriodAsync(employeeId, year, first, last, ct))
                return true;
        }
        return false;
    }

    private static bool IsReimbursement(PayrunComponentBreakdown b) =>
        string.Equals(b.ComponentCode, "REIMBURSEMENT", StringComparison.OrdinalIgnoreCase);

    private static SalaryComponentInput MapBreakdownToEngineInput(PayrunComponentBreakdown b) =>
        new(
            ComponentId: b.SalaryComponentId ?? Guid.Empty,
            Code: b.ComponentCode,
            Amount: b.FullAmount,
            IsTaxable: b.IsTaxable,
            ConsiderForEpf: b.ConsiderForEpf,
            ConsiderForEsi: b.ConsiderForEsi,
            // FnF one-time entries (Bonus, Commission, Leave Encash, Gratuity,
            // Notice Pay) are flat — never prorated to LWD even when LOP is set.
            CalculateOnProRata: !b.IsOneTimeEarning && b.CalculateOnProRata,
            IsFlat: false,
            ShowInPayslip: b.ShowInPayslip);

    // ── Shared helpers used by both UpdateFnfRunCommand and ApprovePayrollRunCommand ──

    /// <summary>
    /// Applies FnF engine result to a tracked PayrunEmployee. Call site must hold
    /// the same pe reference that the cost calculator will read.
    /// </summary>
    internal static void ApplyToPayrunEmployee(PayrunEmployee pe, FnfEngineResult fnf, Guid actorId)
    {
        PayrollResult result = fnf.Engine;
        pe.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            taxableGrossPay: result.Gross.TaxableGrossWage,
            netPay: fnf.NetPayWithAdjustments,
            taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
            benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
            reimbursementsAmount: fnf.ReimbursementsAmount,
            employeePf: result.PF.EmployeeContribution,
            employerPf: result.PF.EPFEmployerContribution,
            employeeEsi: result.ESI.EmployeeContribution,
            employerEsi: result.ESI.EmployerContribution,
            ptAmount: result.PT.Amount,
            tdsAmount: pe.TdsOverrideAmount ?? result.TDS.MonthlyTDS,
            lwfEmployeeAmount: result.LWF.EmployeeAmount,
            lwfEmployerAmount: result.LWF.EmployerAmount,
            gratuityAmount: result.Gratuity.MonthlyAccrual,
            epsAmount: result.PF.EPSEmployerContribution,
            monthlyCTC: pe.MonthlyCTC,
            actorId: actorId);
    }

    /// <summary>
    /// Builds a TdsWorksheet for a FnF computation. Uses remainingMonthsInFy=1
    /// because the engine always ran with MonthsRemainingInFY=1 for TDS closure.
    /// </summary>
    internal static TdsWorksheet BuildWorksheet(PayrollRun run, PayrunEmployee pe, FnfEngineResult fnf, Guid createdBy)
    {
        PayrollResult result = fnf.Engine;
        decimal tdsThisMonth = pe.TdsOverrideAmount ?? result.TDS.MonthlyTDS;
        return TdsWorksheet.Create(
            payrollRunId: run.Id,
            employeeId: pe.EmployeeId,
            tenantId: pe.TenantId,
            fiscalYear: fnf.LwdFiscalYear,
            annualProjectedIncome: result.TDS.TaxableIncome + fnf.StaticConfig.StandardDeduction,
            standardDeduction: fnf.StaticConfig.StandardDeduction,
            taxableIncome: result.TDS.TaxableIncome,
            taxBeforeRebate: result.TDS.TaxBeforeRebate,
            rebate87A: result.TDS.Rebate87AApplied
                ? Math.Min(result.TDS.TaxBeforeRebate, fnf.StaticConfig.Rebate87AAmount)
                : 0m,
            surcharge: result.TDS.Surcharge,
            cess: result.TDS.Cess,
            annualTaxLiability: result.TDS.AnnualProjectedTax,
            ytdTdsDeducted: fnf.YtdTdsDeducted,
            remainingMonthsInFy: 1, // engine ran with MonthsRemainingInFY=1 for TDS closure
            tdsThisMonth: tdsThisMonth,
            hasPanOverride: result.TDS.HasPanOverride,
            createdBy: createdBy);
    }
}
