using Payroll.Domain.Entities;

namespace Payroll.Application.Services;

// Single source of truth for run-level financial totals. Must be called by every
// handler that mutates payrun_employees so PayrollRun.PayrollCost stays consistent
// across initiation, LOP edits, one-time entries, and reimbursement imports.
public sealed class PayrollCostCalculator : IPayrollCostCalculator
{
    public PayrollCostSnapshot Calculate(IReadOnlyList<PayrunEmployee> activeEmployees)
    {
        decimal totalGross = 0m;
        decimal totalNet = 0m;
        decimal totalEmployerPf = 0m;
        decimal totalEmployerEps = 0m;
        decimal totalEmployerEsi = 0m;
        decimal totalLwfEmployer = 0m;
        decimal totalGratuity = 0m;
        decimal totalTds = 0m;
        decimal totalPt = 0m;

        foreach (PayrunEmployee e in activeEmployees)
        {
            totalGross += e.GrossPay;
            totalNet += e.NetPay;
            totalEmployerPf += e.EmployerPf;
            totalEmployerEps += e.EpsAmount;
            totalEmployerEsi += e.EmployerEsi;
            totalLwfEmployer += e.LwfEmployerAmount;
            totalGratuity += e.GratuityAmount;
            totalTds += e.TdsAmount;
            totalPt += e.PtAmount;
        }

        decimal payrollCost = totalGross
            + totalEmployerPf
            + totalEmployerEps
            + totalEmployerEsi
            + totalLwfEmployer
            + totalGratuity;

        return new PayrollCostSnapshot(
            TotalGross: totalGross,
            TotalNet: totalNet,
            TotalEmployerPf: totalEmployerPf,
            TotalEmployerEps: totalEmployerEps,
            TotalEmployerEsi: totalEmployerEsi,
            TotalLwfEmployer: totalLwfEmployer,
            TotalGratuity: totalGratuity,
            TotalTds: totalTds,
            TotalPt: totalPt,
            EmployeeCount: activeEmployees.Count,
            PayrollCost: payrollCost);
    }
}
