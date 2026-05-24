using MediatR;
using Payroll.Application.Interfaces;

namespace Payroll.Application.Commands.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<Unit>, IAllowWithoutTenant;
