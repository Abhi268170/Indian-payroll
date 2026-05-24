using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record ListEmployeesQuery(
    PaginationParams Pagination,
    string? Status = null,
    string? Search = null) : IRequest<PagedResult<EmployeeListItemDto>>
{
    public ListEmployeesQuery() : this(new PaginationParams()) { }
}

public sealed class ListEmployeesHandler(
    IEmployeeRepository repo,
    IDepartmentRepository deptRepo,
    IDesignationRepository desigRepo,
    IWorkLocationRepository wlRepo)
    : IRequestHandler<ListEmployeesQuery, PagedResult<EmployeeListItemDto>>
{
    public async Task<PagedResult<EmployeeListItemDto>> Handle(
        ListEmployeesQuery request,
        CancellationToken ct)
    {
        // Whole-tenant load with in-memory paging. Indian payroll tenants are
        // typically <1000 employees; the cost of streaming all rows is small
        // compared to maintaining a paged repo method per listing. Switch to
        // a paged repo method when a tenant crosses ~5000 employees.
        IReadOnlyList<Employee> all = await repo.ListAsync(ct);
        IReadOnlyList<Department> depts = await deptRepo.ListAsync(ct);
        IReadOnlyList<Designation> desigs = await desigRepo.ListAsync(ct);
        IReadOnlyList<WorkLocation> wls = await wlRepo.ListAsync(ct);

        Dictionary<Guid, string> deptNames = depts.ToDictionary(d => d.Id, d => d.Name);
        Dictionary<Guid, string> desigNames = desigs.ToDictionary(d => d.Id, d => d.Name);
        Dictionary<Guid, string> wlNames = wls.ToDictionary(w => w.Id, w => w.Name);

        IEnumerable<Employee> filtered = all;
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "All")
            filtered = filtered.Where(e => string.Equals(e.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string q = request.Search.Trim();
            filtered = filtered.Where(e =>
                e.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.WorkEmail.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.EmployeeCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (deptNames.GetValueOrDefault(e.DepartmentId) ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        var filteredList = filtered.OrderBy(e => e.EmployeeCode).ToList();
        List<EmployeeListItemDto> page = filteredList
            .Skip(request.Pagination.SkipCount)
            .Take(request.Pagination.TakeCount)
            .Select(e => new EmployeeListItemDto(
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
                e.EmploymentType.ToString()))
            .ToList();

        return new PagedResult<EmployeeListItemDto>(
            page, filteredList.Count, request.Pagination.NormalizedPage, request.Pagination.NormalizedSize);
    }
}
