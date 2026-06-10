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
    IEmployeeExitRepository exitRepo,
    IDesignationRepository designationRepo,
    IDepartmentRepository departmentRepo)
    : IRequestHandler<GetPayrollRunEmployeesQuery, PagedResult<PayrunEmployeeDto>>
{
    public async Task<PagedResult<PayrunEmployeeDto>> Handle(GetPayrollRunEmployeesQuery req, CancellationToken ct)
    {
        Domain.Entities.PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        PaginationParams pagination = req.Pagination ?? new PaginationParams();

        // WI-24: for FnF runs, surface each employee's LWD + exit reason inline
        // so HR can verify a bulk settlement without opening every row.
        bool isFnf = run.Type == PayrollRunType.FinalSettlement
                  || run.Type == PayrollRunType.BulkFinalSettlement;
        Dictionary<Guid, Domain.Entities.EmployeeExit> exitByEmployee = isFnf
            ? (await exitRepo.GetByFnfRunIdsAsync(new[] { req.RunId }, ct)).ToDictionary(e => e.EmployeeId)
            : new();

        IReadOnlyList<Domain.Entities.PayrunEmployee> payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);

        List<Domain.Entities.PayrunEmployee> filteredEmps = req.Filter?.ToLowerInvariant() switch
        {
            "active" => payrunEmps.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList(),
            "skipped" => payrunEmps.Where(e => e.Status == PayrunEmployeeStatus.Skipped).ToList(),
            _ => payrunEmps.ToList(),
        };

        IReadOnlyList<Domain.Entities.Designation> designations = await designationRepo.ListAsync(ct);
        IReadOnlyList<Domain.Entities.Department> departments = await departmentRepo.ListAsync(ct);
        Dictionary<Guid, string> designationMap = designations.ToDictionary(d => d.Id, d => d.Name);
        Dictionary<Guid, string> departmentMap = departments.ToDictionary(d => d.Id, d => d.Name);

        IReadOnlyList<Domain.Entities.Employee> employees = await employeeRepo.GetManyByIdsAsync(
            filteredEmps.Select(e => e.EmployeeId), ct);
        Dictionary<Guid, Domain.Entities.Employee> employeeMap = employees.ToDictionary(e => e.Id);

        List<Domain.Entities.PayrunEmployee> ordered = filteredEmps
            .Where(pe => employeeMap.ContainsKey(pe.EmployeeId))
            .OrderBy(pe => employeeMap[pe.EmployeeId].EmployeeCode)
            .ToList();

        List<PayrunEmployeeDto> pageRows = ordered
            .Skip(pagination.SkipCount)
            .Take(pagination.TakeCount)
            .Select(pe =>
            {
                Domain.Entities.Employee emp = employeeMap[pe.EmployeeId];
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
                    VpfAmount: pe.VpfAmount,
                    EmployeeEsi: pe.EmployeeEsi,
                    PtAmount: pe.PtAmount,
                    LwfEmployeeAmount: pe.LwfEmployeeAmount,
                    TdsAmount: pe.TdsAmount,
                    TdsOverrideAmount: pe.TdsOverrideAmount,
                    SkipReason: pe.SkipReason,
                    LastWorkingDay: exitByEmployee.GetValueOrDefault(pe.EmployeeId)?.LastWorkingDay,
                    ExitReason: exitByEmployee.GetValueOrDefault(pe.EmployeeId)?.Reason.ToString());
            })
            .ToList();

        return new PagedResult<PayrunEmployeeDto>(
            pageRows, ordered.Count, pagination.NormalizedPage, pagination.NormalizedSize);
    }
}
