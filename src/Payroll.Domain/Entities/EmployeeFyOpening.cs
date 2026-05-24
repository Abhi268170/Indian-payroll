using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

// Tracks same-employer salary/TDS paid in months before this system was adopted (migration opening balance).
// Distinct from PriorEmployerYtd which covers a *different* employer (Form 12B scenario).
public sealed class EmployeeFyOpening : AuditableEntity
{
    private EmployeeFyOpening() { }

    public Guid EmployeeId { get; private set; }
    public int FiscalYear { get; private set; }   // start year, e.g. 2024 for FY2024-25
    public int MonthsCount { get; private set; }  // how many months this covers (audit only)
    public decimal GrossSalary { get; private set; }
    public decimal TdsDeducted { get; private set; }
    public decimal PfDeducted { get; private set; }

    public static EmployeeFyOpening Create(
        Guid employeeId,
        int fiscalYear,
        int monthsCount,
        decimal grossSalary,
        decimal tdsDeducted,
        decimal pfDeducted,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            FiscalYear = fiscalYear,
            MonthsCount = monthsCount,
            GrossSalary = grossSalary,
            TdsDeducted = tdsDeducted,
            PfDeducted = pfDeducted,
            CreatedBy = createdBy
        };

    public void Update(
        int monthsCount,
        decimal grossSalary,
        decimal tdsDeducted,
        decimal pfDeducted,
        Guid updatedBy)
    {
        MonthsCount = monthsCount;
        GrossSalary = grossSalary;
        TdsDeducted = tdsDeducted;
        PfDeducted = pfDeducted;
        SetUpdated(updatedBy);
    }
}
