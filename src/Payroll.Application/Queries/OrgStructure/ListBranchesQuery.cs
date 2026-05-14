using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public record ListBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

internal sealed class ListBranchesHandler(IBranchRepository branches)
    : IRequestHandler<ListBranchesQuery, IReadOnlyList<BranchDto>>
{
    public async Task<IReadOnlyList<BranchDto>> Handle(ListBranchesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Branch> all = await branches.GetAllAsync(cancellationToken);
        return all.Select(b => new BranchDto(b.Id, b.Name, b.State)).ToList();
    }
}
