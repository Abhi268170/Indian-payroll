using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record CreateEarningCommand(
    string Name,
    string NameInPayslip,
    string? Code,
    string EarningType,
    string PayType,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    bool IsTaxable,
    bool ConsiderForEpf,
    string EpfInclusionRule,
    bool ConsiderForEsi,
    bool CalculateOnProRata,
    bool ShowInPayslip,
    Guid ActorId,
    bool IsOneTime = false
) : IRequest<Guid>;
