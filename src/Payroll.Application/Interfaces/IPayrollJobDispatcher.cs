namespace Payroll.Application.Interfaces;

public interface IPayrollJobDispatcher
{
    void EnqueueGeneratePayslips(Guid payrollRunId);
    void EnqueueGeneratePayslipsThenNotify(Guid payrollRunId);
}
