using MediatR;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Users;

internal sealed class CreateUserHandler(
    IUserService userService,
    ITenantContext tenantContext) : IRequestHandler<CreateUserCommand, Guid>
{
    public Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        userService.CreateTenantUserAsync(
            request.Email,
            request.Password,
            request.Role,
            tenantContext.TenantId,
            cancellationToken);
}
