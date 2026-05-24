using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayslipRepository(PayrollDbContext db) : IPayslipRepository
{
    public Task<Payslip?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Payslips.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payslip?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default) =>
        db.Payslips.FirstOrDefaultAsync(p => p.PayrollRunId == payrollRunId && p.EmployeeId == employeeId, ct);

    public Task<IReadOnlyList<Payslip>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        db.Payslips
            .Where(p => p.PayrollRunId == payrollRunId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Payslip>>(t => t.Result, ct);

    public Task<IReadOnlyList<Payslip>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default) =>
        db.Payslips
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Payslip>>(t => t.Result, ct);

    public async Task AddAsync(Payslip payslip, CancellationToken ct = default) =>
        await db.Payslips.AddAsync(payslip, ct);

    public async Task AddRangeAsync(IEnumerable<Payslip> payslips, CancellationToken ct = default) =>
        await db.Payslips.AddRangeAsync(payslips, ct);

    public void Update(Payslip payslip) =>
        db.Payslips.Update(payslip);
}
