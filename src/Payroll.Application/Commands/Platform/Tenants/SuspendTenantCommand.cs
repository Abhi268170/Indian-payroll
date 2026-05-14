using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

public record SuspendTenantCommand(Guid TenantId) : IRequest, IAllowWithoutTenant;
