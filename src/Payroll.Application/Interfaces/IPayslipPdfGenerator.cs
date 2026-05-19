using Payroll.Application.DTOs;

namespace Payroll.Application.Interfaces;

public interface IPayslipPdfGenerator
{
    byte[] Generate(PayslipData data);
}
