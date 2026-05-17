using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryStructureTemplates;

public sealed record ListSalaryStructureTemplatesQuery : IRequest<List<SalaryStructureTemplateSummaryDto>>;

public sealed class ListSalaryStructureTemplatesHandler(
    ISalaryStructureTemplateRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<ListSalaryStructureTemplatesQuery, List<SalaryStructureTemplateSummaryDto>>
{
    public async Task<List<SalaryStructureTemplateSummaryDto>> Handle(
        ListSalaryStructureTemplatesQuery req, CancellationToken ct)
    {
        List<Domain.Entities.SalaryStructureTemplate> templates =
            await repo.ListByTenantAsync(tenantContext.TenantId, ct);

        return templates.Select(t => new SalaryStructureTemplateSummaryDto(
            t.Id, t.Name, t.Description, t.IsActive, t.Components.Count))
            .ToList();
    }
}
