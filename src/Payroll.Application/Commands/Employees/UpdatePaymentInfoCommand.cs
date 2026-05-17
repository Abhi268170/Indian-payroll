using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record UpdatePaymentInfoCommand(
    Guid EmployeeId,
    string PaymentMode,
    string? AccountHolderName,
    string? BankName,
    string? AccountType,
    string? AccountNumber,
    string? IFSC,
    Guid ActorId) : IRequest;

internal sealed class UpdatePaymentInfoValidator : AbstractValidator<UpdatePaymentInfoCommand>
{
    private static readonly HashSet<string> BankModes = new()
        { "BankTransfer", "DirectDeposit" };

    public UpdatePaymentInfoValidator()
    {
        RuleFor(x => x.PaymentMode).NotEmpty()
            .Must(m => Enum.TryParse<PaymentMode>(m, out _)).WithMessage("Invalid payment mode.");

        When(x => BankModes.Contains(x.PaymentMode), () =>
        {
            RuleFor(x => x.AccountHolderName).NotEmpty();
            RuleFor(x => x.BankName).NotEmpty();
            RuleFor(x => x.AccountType)
                .Must(t => t != null && Enum.TryParse<AccountType>(t, out _))
                .WithMessage("Account type required.");
            RuleFor(x => x.AccountNumber).NotEmpty();
            RuleFor(x => x.IFSC).NotEmpty().Length(11).WithMessage("IFSC must be 11 characters.");
        });
    }
}

public sealed class UpdatePaymentInfoHandler(
    IEmployeeRepository repo,
    IEncryptionService enc,
    IUnitOfWork uow)
    : IRequestHandler<UpdatePaymentInfoCommand>
{
    public async Task Handle(UpdatePaymentInfoCommand req, CancellationToken ct)
    {
        Domain.Entities.Employee employee = await repo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        string? encAcct = req.AccountNumber is not null ? enc.Encrypt(req.AccountNumber) : null;
        string? encIfsc = req.IFSC is not null ? enc.Encrypt(req.IFSC) : null;
        AccountType? acctType = req.AccountType is not null ? Enum.Parse<AccountType>(req.AccountType) : null;

        employee.UpdatePaymentInfo(
            Enum.Parse<PaymentMode>(req.PaymentMode),
            req.AccountHolderName,
            req.BankName,
            acctType,
            encAcct,
            encIfsc,
            req.ActorId);

        repo.Update(employee);
        await uow.SaveChangesAsync(ct);
    }
}
