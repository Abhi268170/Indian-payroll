using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollRunEmployeesQuery(Guid RunId, string? Filter = null) : IRequest<IReadOnlyList<PayrunEmployeeDto>>;

public sealed class GetPayrollRunEmployeesHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IDesignationRepository designationRepo,
    IDepartmentRepository departmentRepo)
    : IRequestHandler<GetPayrollRunEmployeesQuery, IReadOnlyList<PayrunEmployeeDto>>
{
    public async Task<IReadOnlyList<PayrunEmployeeDto>> Handle(GetPayrollRunEmployeesQuery req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        _ = run; // accessed for existence check

        var payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);

        var filteredEmps = req.Filter?.ToLowerInvariant() switch
        {
            "active" => payrunEmps.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList(),
            "skipped" => payrunEmps.Where(e => e.Status == PayrunEmployeeStatus.Skipped).ToList(),
            _ => payrunEmps.ToList(),
        };

        var designations = await designationRepo.ListAsync(ct);
        var departments = await departmentRepo.ListAsync(ct);
        var designationMap = designations.ToDictionary(d => d.Id, d => d.Name);
        var departmentMap = departments.ToDictionary(d => d.Id, d => d.Name);

        IReadOnlyList<Domain.Entities.Employee> employees = await employeeRepo.GetManyByIdsAsync(
            filteredEmps.Select(e => e.EmployeeId), ct);
        Dictionary<Guid, Domain.Entities.Employee> employeeMap = employees.ToDictionary(e => e.Id);

        var result = new List<PayrunEmployeeDto>(filteredEmps.Count);
        foreach (var pe in filteredEmps)
        {
            if (!employeeMap.TryGetValue(pe.EmployeeId, out Domain.Entities.Employee? emp)) continue;

            result.Add(new PayrunEmployeeDto(
                EmployeeId: emp.Id,
                EmployeeCode: emp.EmployeeCode,
                EmployeeName: emp.FullName,
                Department: departmentMap.GetValueOrDefault(emp.DepartmentId, string.Empty),
                Designation: designationMap.GetValueOrDefault(emp.DesignationId, string.Empty),
                Status: pe.Status.ToString(),
                LopDays: pe.LopDays,
                BaseDays: pe.BaseDays,
                GrossPay: pe.GrossPay,
                NetPay: pe.NetPay,
                EmployeePf: pe.EmployeePf,
                EmployeeEsi: pe.EmployeeEsi,
                PtAmount: pe.PtAmount,
                LwfEmployeeAmount: pe.LwfEmployeeAmount,
                TdsAmount: pe.TdsAmount,
                TdsOverrideAmount: pe.TdsOverrideAmount,
                SkipReason: pe.SkipReason));
        }

        return result.OrderBy(r => r.EmployeeCode).ToList();
    }
}
