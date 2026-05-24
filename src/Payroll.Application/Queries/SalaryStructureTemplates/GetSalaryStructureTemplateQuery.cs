using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryStructureTemplates;

public sealed record GetSalaryStructureTemplateQuery(Guid Id)
    : IRequest<SalaryStructureTemplateDetailDto?>;

public sealed class GetSalaryStructureTemplateHandler(
    ISalaryStructureTemplateRepository repo,
    ITenantContext tenantContext)
    : IRequestHandler<GetSalaryStructureTemplateQuery, SalaryStructureTemplateDetailDto?>
{
    public async Task<SalaryStructureTemplateDetailDto?> Handle(
        GetSalaryStructureTemplateQuery req, CancellationToken ct)
    {
        SalaryStructureTemplate? template = await repo.GetByIdWithComponentsAsync(req.Id, ct);
        if (template is null || template.TenantId != tenantContext.TenantId) return null;

        List<SalaryStructureComponentDto> components = template.Components
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new SalaryStructureComponentDto(
                c.ComponentId,
                c.Component?.Name ?? string.Empty,
                c.Component?.Code ?? string.Empty,
                c.Component?.Category ?? default,
                c.Component?.IsSystemComponent ?? false,
                c.FormulaType,
                c.FixedAmount,
                c.Percentage,
                c.DisplayOrder))
            .ToList();

        return new SalaryStructureTemplateDetailDto(
            template.Id, template.Name, template.Description, template.IsActive, components);
    }
}
