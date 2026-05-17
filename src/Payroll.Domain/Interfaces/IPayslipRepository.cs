using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IPayslipRepository
{
    Task<Payslip?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<Payslip>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
    Task AddAsync(Payslip payslip, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Payslip> payslips, CancellationToken ct = default);
    void Update(Payslip payslip);
}
