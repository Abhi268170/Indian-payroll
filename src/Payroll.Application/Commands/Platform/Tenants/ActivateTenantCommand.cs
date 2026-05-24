using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

public record ActivateTenantCommand(Guid TenantId) : IRequest, IAllowWithoutTenant;
