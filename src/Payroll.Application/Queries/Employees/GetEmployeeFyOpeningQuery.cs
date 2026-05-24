using MediatR;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record EmployeeFyOpeningDto(
    Guid EmployeeId,
    int FiscalYear,
    int MonthsCount,
    decimal GrossSalary,
    decimal TdsDeducted,
    decimal PfDeducted);

public record GetEmployeeFyOpeningQuery(Guid EmployeeId, int FiscalYear) : IRequest<EmployeeFyOpeningDto?>;

public sealed class GetEmployeeFyOpeningHandler(IEmployeeFyOpeningRepository repo)
    : IRequestHandler<GetEmployeeFyOpeningQuery, EmployeeFyOpeningDto?>
{
    public async Task<EmployeeFyOpeningDto?> Handle(GetEmployeeFyOpeningQuery req, CancellationToken ct)
    {
        var entity = await repo.GetAsync(req.EmployeeId, req.FiscalYear, ct);
        if (entity is null) return null;
        return new EmployeeFyOpeningDto(
            entity.EmployeeId,
            entity.FiscalYear,
            entity.MonthsCount,
            entity.GrossSalary,
            entity.TdsDeducted,
            entity.PfDeducted);
    }
}
