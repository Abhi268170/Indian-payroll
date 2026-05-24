using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Commands.Auth;

public record SetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Unit>, IAllowWithoutTenant;
