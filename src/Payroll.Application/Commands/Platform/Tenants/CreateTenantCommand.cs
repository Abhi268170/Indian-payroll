using MediatR;

namespace Payroll.Application.Commands.Platform.Tenants;

public record CreateTenantCommand(string DisplayName, string Slug) : IRequest<Guid>;
