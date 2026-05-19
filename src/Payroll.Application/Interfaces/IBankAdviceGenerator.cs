using Payroll.Application.DTOs;

namespace Payroll.Application.Interfaces;

public interface IBankAdviceGenerator
{
    byte[] Generate(BankAdviceDto data);
}
