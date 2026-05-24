using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

public record ResendSetupEmailCommand(Guid TenantId) : IRequest, IAllowWithoutTenant;
