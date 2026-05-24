using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPayrollRunEmployeesQuery(
    Guid RunId,
    string? Filter = null,
    PaginationParams? Pagination = null) : IRequest<PagedResult<PayrunEmployeeDto>>;

public sealed class GetPayrollRunEmployeesHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IDesignationRepository designationRepo,
    IDepartmentRepository departmentRepo)
    : IRequestHandler<GetPayrollRunEmployeesQuery, PagedResult<PayrunEmployeeDto>>
{
    public async Task<PagedResult<PayrunEmployeeDto>> Handle(GetPayrollRunEmployeesQuery req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        _ = run; // accessed for existence check

        var pagination = req.Pagination ?? new PaginationParams();

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

        var ordered = filteredEmps
            .Where(pe => employeeMap.ContainsKey(pe.EmployeeId))
            .OrderBy(pe => employeeMap[pe.EmployeeId].EmployeeCode)
            .ToList();

        var pageRows = ordered
            .Skip(pagination.SkipCount)
            .Take(pagination.TakeCount)
            .Select(pe =>
            {
                var emp = employeeMap[pe.EmployeeId];
                return new PayrunEmployeeDto(
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
                    SkipReason: pe.SkipReason);
            })
            .ToList();

        return new PagedResult<PayrunEmployeeDto>(
            pageRows, ordered.Count, pagination.NormalizedPage, pagination.NormalizedSize);
    }
}
