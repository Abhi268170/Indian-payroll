using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record ListEmployeesQuery : IRequest<IReadOnlyList<EmployeeListItemDto>>;

public sealed class ListEmployeesHandler(
    IEmployeeRepository repo,
    IDepartmentRepository deptRepo,
    IDesignationRepository desigRepo,
    IWorkLocationRepository wlRepo)
    : IRequestHandler<ListEmployeesQuery, IReadOnlyList<EmployeeListItemDto>>
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> Handle(
        ListEmployeesQuery request,
        CancellationToken ct)
    {
        IReadOnlyList<Employee> employees = await repo.ListAsync(ct);
        IReadOnlyList<Department> depts = await deptRepo.ListAsync(ct);
        IReadOnlyList<Designation> desigs = await desigRepo.ListAsync(ct);
        IReadOnlyList<WorkLocation> wls = await wlRepo.ListAsync(ct);

        Dictionary<Guid, string> deptNames = depts.ToDictionary(d => d.Id, d => d.Name);
        Dictionary<Guid, string> desigNames = desigs.ToDictionary(d => d.Id, d => d.Name);
        Dictionary<Guid, string> wlNames = wls.ToDictionary(w => w.Id, w => w.Name);

        return employees.OrderBy(e => e.EmployeeCode).Select(e => new EmployeeListItemDto(
            e.Id,
            e.EmployeeCode,
            e.FullName,
            e.WorkEmail,
            e.MobileNumber,
            e.Status.ToString(),
            deptNames.GetValueOrDefault(e.DepartmentId),
            desigNames.GetValueOrDefault(e.DesignationId),
            wlNames.GetValueOrDefault(e.WorkLocationId),
            e.DateOfJoining.ToString("yyyy-MM-dd"),
            e.ProfileComplete,
            e.EnablePortalAccess,
            e.EmploymentType.ToString()
        )).ToList();
    }
}
