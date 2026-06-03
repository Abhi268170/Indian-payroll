using Payroll.Application.Interfaces;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryRevisions;

// Lookups needed to resolve + dedup import rows, loaded once per import pass.
internal sealed class SalaryRevisionImportContext
{
    public required IReadOnlyDictionary<string, Employee> EmployeesByCode { get; init; }
    public required IReadOnlyDictionary<string, Guid> TemplatesByName { get; init; }
    public required IReadOnlyDictionary<Guid, decimal> ActiveCtcByEmployee { get; init; }
    public required IReadOnlyDictionary<Guid, HashSet<(int Month, int Year)>> ExistingPayoutsByEmployee { get; init; }

    public static async Task<SalaryRevisionImportContext> LoadAsync(
        IReadOnlyList<SalaryRevisionImportRow> rows,
        IEmployeeRepository employeeRepo,
        ISalaryStructureTemplateRepository templateRepo,
        IEmployeeSalaryStructureRepository salaryStructureRepo,
        ISalaryRevisionRepository revisionRepo,
        ITenantContext tenantContext,
        CancellationToken ct)
    {
        List<string> codes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeNumber))
            .Select(r => r.EmployeeNumber!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByCodesAsync(codes, ct);
        Dictionary<string, Employee> employeesByCode =
            employees.ToDictionary(e => e.EmployeeCode, e => e, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, Guid> templatesByName =
            (await templateRepo.ListByTenantAsync(tenantContext.TenantId, ct))
            .ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<Guid, decimal> activeCtc = new Dictionary<Guid, decimal>();
        Dictionary<Guid, HashSet<(int, int)>> existingPayouts = new Dictionary<Guid, HashSet<(int, int)>>();
        foreach (Employee employee in employees)
        {
            EmployeeSalaryStructure? active = await salaryStructureRepo.GetActiveAsync(employee.Id, ct);
            if (active is not null)
                activeCtc[employee.Id] = active.AnnualCTC;

            IReadOnlyList<SalaryRevision> existing = await revisionRepo.GetByEmployeeAsync(employee.Id, ct);
            existingPayouts[employee.Id] = existing
                .Select(r => (r.PayoutMonth, r.PayoutYear))
                .ToHashSet();
        }

        return new SalaryRevisionImportContext
        {
            EmployeesByCode = employeesByCode,
            TemplatesByName = templatesByName,
            ActiveCtcByEmployee = activeCtc,
            ExistingPayoutsByEmployee = existingPayouts,
        };
    }
}
