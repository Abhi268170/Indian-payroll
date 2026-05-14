using Payroll.Domain.Enums;

namespace Payroll.Application.DTOs;

public sealed record BranchDto(Guid Id, string Name, IndianState State);
