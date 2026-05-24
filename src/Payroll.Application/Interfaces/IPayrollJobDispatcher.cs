namespace Payroll.Application.Interfaces;

public interface IPayrollJobDispatcher
{
    void EnqueueGeneratePayslips(Guid payrollRunId, Guid tenantId);
    void EnqueueGeneratePayslipsThenNotify(Guid payrollRunId, Guid tenantId);
}
