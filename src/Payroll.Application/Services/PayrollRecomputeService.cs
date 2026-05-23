using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using System.Text.Json;

namespace Payroll.Application.Services;

public sealed class PayrollRecomputeService(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IWorkLocationRepository workLocationRepo,
    IPayScheduleRepository payScheduleRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    ISalaryComponentRepository salaryComponentRepo)
    : IPayrollRecomputeService
{
    public async Task<RecomputeResult> RecomputeEmployeeAsync(
        Guid runId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        PayrollRun run = await runRepo.GetByIdAsync(runId, ct)
            ?? throw new NotFoundException($"Payroll run {runId} not found.");

        PayrunEmployee payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(runId, employeeId, ct)
            ?? throw new NotFoundException($"Employee {employeeId} not in run {runId}.");

        Employee employee = await employeeRepo.GetByIdAsync(employeeId, ct)
            ?? throw new NotFoundException($"Employee {employeeId} not found.");

        IReadOnlyList<PayrunComponentBreakdown> breakdowns =
            await breakdownRepo.GetByRunAndEmployeeAsync(runId, employeeId, ct);

        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException("Payroll run is missing statutory config snapshot.");
        StatutoryConfig staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

        WorkLocation? workLocation = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
        string workStateCode = workLocation?.State.ToString() ?? "MH";

        PaySchedule paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        int salaryDivisor = PayScheduleHelpers.GetDivisor(
            engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth,
            run.PayPeriod.Year, run.PayPeriod.Month);

        (decimal currentYtdGross, decimal currentYtdTds) = await LoadCurrentYtdAsync(employeeId, run.PayPeriod.FiscalYear, ct);

        // Partition breakdowns by role. Classification by linked component's
        // Category avoids depending on a flag column.
        List<Guid> componentIds = breakdowns
            .Where(b => b.SalaryComponentId.HasValue)
            .Select(b => b.SalaryComponentId!.Value)
            .Distinct()
            .ToList();
        Dictionary<Guid, ComponentCategory> categoryById = componentIds.Count == 0
            ? new Dictionary<Guid, ComponentCategory>()
            : (await salaryComponentRepo.GetByIdsAsync(componentIds, ct))
                .ToDictionary(c => c.Id, c => c.Category);

        var reimbursementRows = new List<PayrunComponentBreakdown>();
        var deductionRows = new List<PayrunComponentBreakdown>();
        var engineRows = new List<PayrunComponentBreakdown>();
        foreach (PayrunComponentBreakdown b in breakdowns)
        {
            if (IsReimbursement(b))
                reimbursementRows.Add(b);
            else if (b.SalaryComponentId.HasValue
                && categoryById.TryGetValue(b.SalaryComponentId.Value, out var cat)
                && cat == ComponentCategory.Deduction)
                deductionRows.Add(b);
            else
                engineRows.Add(b);
        }

        decimal reimbursementsAmount = reimbursementRows.Sum(b => b.FullAmount);
        decimal deductionsAmount = deductionRows.Sum(b => b.FullAmount);

        IReadOnlyList<SalaryComponentInput> components = engineRows
            .Select(MapToEngineInput)
            .ToList();

        decimal basicWage = engineRows
            .FirstOrDefault(b => b.ComponentCode == "BASICSALARY")?.FullAmount ?? 0m;

        bool hasPan = !string.IsNullOrWhiteSpace(employee.EncryptedPAN);
        var (hyIndex, hyTotal) = run.PayPeriod.HalfYearPosition(employee.DateOfJoining);

        var empInput = new EmployeeInput(
            EmployeeId: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            WorkStateCode: workStateCode,
            EpfEnabled: employee.EpfEnabled,
            IsESIExempt: !employee.EsiEnabled,
            IsPWD: employee.IsPWD,
            MonthlyCTC: payrunEmp.MonthlyCTC,
            Components: components,
            LOPDays: payrunEmp.LopDays,
            WorkingDaysInMonth: payrunEmp.BaseDays,
            VPFAmount: 0m,
            PriorEmployerYTDTaxableIncome: 0m,
            PriorEmployerYTDTDSDeducted: 0m,
            PriorEmployerYTDPF: 0m,
            HalfYearMonthIndex: hyIndex,
            HalfYearTotalMonths: hyTotal,
            BasicWage: basicWage,
            HasPan: hasPan,
            CurrentEmployerYTDGross: currentYtdGross,
            CurrentEmployerYTDTDSDeducted: currentYtdTds);

        var runInput = new PayrollRunInput(
            Year: run.PayPeriod.Year,
            Month: run.PayPeriod.Month,
            CalendarDaysInMonth: payrunEmp.BaseDays,
            SalaryDivisor: salaryDivisor,
            MonthsRemainingInFY: run.PayPeriod.MonthsRemainingInFiscalYear(),
            FiscalYearLabel: run.PayPeriod.FiscalYearLabel);

        PayrollResult result = PayrollEngine.Compute([empInput], runInput, staticConfig)[0];

        // Sync stored prorated amounts on non-reimbursement breakdowns so the
        // payslip line items match the engine pass.
        foreach (PayrunComponentBreakdown bd in engineRows)
        {
            ComponentAmountResult? computed = result.Gross.ComponentBreakdown
                .FirstOrDefault(c => c.ComponentId == (bd.SalaryComponentId ?? Guid.Empty));
            if (computed is not null)
                bd.UpdateAmounts(computed.FullAmount, computed.ProratedAmount);
        }

        await UpsertTdsWorksheetAsync(run, payrunEmp, result, staticConfig, currentYtdTds, ct);

        return new RecomputeResult(
            Engine: result,
            ReimbursementsAmount: reimbursementsAmount,
            DeductionsAmount: deductionsAmount,
            NetPayWithAdjustments: result.NetPay + reimbursementsAmount - deductionsAmount);
    }

    private async Task<(decimal Gross, decimal Tds)> LoadCurrentYtdAsync(
        Guid employeeId, int fiscalYear, CancellationToken ct)
    {
        Dictionary<Guid, (decimal YtdGross, decimal YtdTds)> ytdMap =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync([employeeId], fiscalYear, ct);

        EmployeeFyOpening? opening = await fyOpeningRepo.GetAsync(employeeId, fiscalYear, ct);
        if (opening is not null)
        {
            ytdMap.TryGetValue(employeeId, out var existing);
            ytdMap[employeeId] = (
                existing.YtdGross + opening.GrossSalary,
                existing.YtdTds + opening.TdsDeducted);
        }

        ytdMap.TryGetValue(employeeId, out var ytd);
        return (ytd.YtdGross, ytd.YtdTds);
    }

    private async Task UpsertTdsWorksheetAsync(
        PayrollRun run,
        PayrunEmployee payrunEmp,
        PayrollResult result,
        StatutoryConfig staticConfig,
        decimal ytdTdsDeducted,
        CancellationToken ct)
    {
        // If an operator set a TDS override on this employee, the deducted amount
        // is the override — not the engine's MonthlyTDS. The worksheet still shows
        // the engine's supporting calculation (taxable income, slabs, surcharge)
        // so the override is auditable against the canonical figure.
        decimal tdsThisMonth = payrunEmp.TdsOverrideAmount ?? result.TDS.MonthlyTDS;

        await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(run.Id, payrunEmp.EmployeeId, ct);
        await tdsWorksheetRepo.AddAsync(TdsWorksheet.Create(
            payrollRunId: run.Id,
            employeeId: payrunEmp.EmployeeId,
            tenantId: payrunEmp.TenantId,
            fiscalYear: run.PayPeriod.FiscalYear,
            annualProjectedIncome: result.TDS.TaxableIncome + staticConfig.StandardDeduction,
            standardDeduction: staticConfig.StandardDeduction,
            taxableIncome: result.TDS.TaxableIncome,
            taxBeforeRebate: result.TDS.TaxBeforeRebate,
            rebate87A: result.TDS.Rebate87AApplied
                ? Math.Min(result.TDS.TaxBeforeRebate, staticConfig.Rebate87AAmount)
                : 0m,
            surcharge: result.TDS.Surcharge,
            cess: result.TDS.Cess,
            annualTaxLiability: result.TDS.AnnualProjectedTax,
            ytdTdsDeducted: ytdTdsDeducted,
            remainingMonthsInFy: run.PayPeriod.MonthsRemainingInFiscalYear(),
            tdsThisMonth: tdsThisMonth,
            hasPanOverride: result.TDS.HasPanOverride,
            createdBy: payrunEmp.UpdatedBy ?? payrunEmp.CreatedBy), ct);
    }

    internal static bool IsReimbursement(PayrunComponentBreakdown b) =>
        b.SalaryComponentId is null
        || string.Equals(b.ComponentCode, "REIMBURSEMENT", StringComparison.OrdinalIgnoreCase);

    internal static SalaryComponentInput MapToEngineInput(PayrunComponentBreakdown b) =>
        new(
            ComponentId: b.SalaryComponentId ?? Guid.Empty,
            Code: b.ComponentCode,
            Amount: b.FullAmount,
            IsTaxable: b.IsTaxable,
            ConsiderForEpf: b.ConsiderForEpf,
            ConsiderForEsi: b.ConsiderForEsi,
            // One-time entries (bonus, commission, ad-hoc deduction) are flat per
            // Zoho rule — proration would shrink the typed amount when the
            // employee has LOP days, which is never what the operator intends.
            CalculateOnProRata: !b.IsOneTimeEarning && b.CalculateOnProRata,
            IsFlat: false,
            ShowInPayslip: b.ShowInPayslip);
}
