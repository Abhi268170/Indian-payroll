using Payroll.Domain.Entities;

namespace Payroll.Application.Services;

public interface IPayrollCostCalculator
{
    PayrollCostSnapshot Calculate(IReadOnlyList<PayrunEmployee> activeEmployees);
}

public sealed record PayrollCostSnapshot(
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerPf,
    decimal TotalEmployerEps,
    decimal TotalEmployerEsi,
    decimal TotalLwfEmployer,
    decimal TotalGratuity,
    decimal TotalTds,
    decimal TotalPt,
    int EmployeeCount,
    decimal PayrollCost);
