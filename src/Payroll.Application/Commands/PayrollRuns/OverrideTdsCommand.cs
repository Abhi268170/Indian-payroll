using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record OverrideTdsCommand(Guid RunId, Guid EmployeeId, decimal OverrideAmount, string Reason, Guid ActorId) : IRequest;

public sealed class OverrideTdsCommandValidator : AbstractValidator<OverrideTdsCommand>
{
    public OverrideTdsCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.OverrideAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Reason is required for TDS override.");
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class OverrideTdsHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IUnitOfWork uow)
    : IRequestHandler<OverrideTdsCommand>
{
    public async Task Handle(OverrideTdsCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot override TDS for a skipped employee.");

        decimal previousTds = payrunEmp.TdsOverrideAmount ?? payrunEmp.TdsAmount;
        payrunEmp.SetTdsOverride(req.OverrideAmount, req.Reason, req.ActorId);

        // Recalculate net pay using override TDS instead of computed TDS
        decimal netPayDelta = previousTds - req.OverrideAmount;
        payrunEmp.UpdateComputedAmounts(
            grossPay: payrunEmp.GrossPay,
            netPay: payrunEmp.NetPay + netPayDelta,
            taxesAmount: payrunEmp.TaxesAmount - (previousTds - req.OverrideAmount),
            benefitsAmount: payrunEmp.BenefitsAmount,
            reimbursementsAmount: payrunEmp.ReimbursementsAmount,
            employeePf: payrunEmp.EmployeePf,
            employerPf: payrunEmp.EmployerPf,
            employeeEsi: payrunEmp.EmployeeEsi,
            employerEsi: payrunEmp.EmployerEsi,
            ptAmount: payrunEmp.PtAmount,
            tdsAmount: req.OverrideAmount,
            lwfEmployeeAmount: payrunEmp.LwfEmployeeAmount,
            lwfEmployerAmount: payrunEmp.LwfEmployerAmount,
            gratuityAmount: payrunEmp.GratuityAmount,
            epsAmount: payrunEmp.EpsAmount,
            monthlyCTC: payrunEmp.MonthlyCTC,
            actorId: req.ActorId);

        payrunEmployeeRepo.Update(payrunEmp);

        // Sync TdsWorksheet to reflect the override
        var worksheet = await tdsWorksheetRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        worksheet?.UpdateTdsThisMonth(req.OverrideAmount, req.ActorId);

        await uow.SaveChangesAsync(ct);
    }
}
