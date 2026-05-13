using MediatR;

namespace Payroll.Application.Commands.Platform.Tenants;

public record SuspendTenantCommand(Guid TenantId) : IRequest;
