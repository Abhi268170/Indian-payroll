using MediatR;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateBranchCommand(string Name, IndianState State, Guid ActorId) : IRequest<Guid>;
