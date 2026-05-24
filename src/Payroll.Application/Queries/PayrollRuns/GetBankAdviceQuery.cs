using MediatR;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetBankAdviceQuery(Guid RunId) : IRequest<BankAdviceDto>;

public sealed class GetBankAdviceHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IEncryptionService encryption)
    : IRequestHandler<GetBankAdviceQuery, BankAdviceDto>
{
    public async Task<BankAdviceDto> Handle(GetBankAdviceQuery req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Approved && run.Status != PayrollRunStatus.Paid)
            throw new InvalidOperationException("Bank advice is only available for Approved or Paid runs.");

        var activePayrunEmps = await payrunEmployeeRepo.GetByRunIdWithStatusAsync(req.RunId, PayrunEmployeeStatus.Active, ct);
        IReadOnlyList<Domain.Entities.Employee> employees = await employeeRepo.GetManyByIdsAsync(
            activePayrunEmps.Select(e => e.EmployeeId), ct);
        Dictionary<Guid, Domain.Entities.Employee> employeeMap = employees.ToDictionary(e => e.Id);

        var rows = new List<BankAdviceRowDto>();
        foreach (var pe in activePayrunEmps)
        {
            if (!employeeMap.TryGetValue(pe.EmployeeId, out Domain.Entities.Employee? employee)) continue;

            if (employee.PaymentMode != PaymentMode.BankTransfer) continue;

            if (employee.EncryptedBankAccount is null || employee.EncryptedIFSC is null) continue;

            string bankAccount = encryption.Decrypt(employee.EncryptedBankAccount);
            string ifsc = encryption.Decrypt(employee.EncryptedIFSC);

            rows.Add(new BankAdviceRowDto(
                EmployeeCode: employee.EmployeeCode,
                EmployeeName: employee.FullName,
                Amount: pe.NetPay,
                BankName: employee.BankName ?? string.Empty,
                BankAccountNo: bankAccount,
                IfscCode: ifsc,
                BeneficiaryName: employee.AccountHolderName ?? employee.FullName));
        }

        string periodLabel = new DateTime(run.PayPeriod.Year, run.PayPeriod.Month, 1).ToString("MMMM yyyy");

        return new BankAdviceDto(req.RunId, periodLabel, rows);
    }
}
