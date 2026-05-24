using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record UpdateSalaryComponentCommand(
    Guid Id,

    // Always editable
    string Name,
    string NameInPayslip,

    // Earning formula (locked after employee association)
    string? FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    bool? IsTaxable,
    bool? ConsiderForEpf,
    string? EpfInclusionRule,
    bool? ConsiderForEsi,
    bool? CalculateOnProRata,
    bool? ShowInPayslip,

    // Deduction
    string? DeductionFrequency,

    // Reimbursement
    decimal? ReimbursementAmount,
    string? UnclaimedHandling,

    Guid ActorId
) : IRequest;
