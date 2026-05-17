namespace Payroll.Infrastructure.Jobs;

// Implemented in U12 (PayslipPdfGenerator)
public sealed class GeneratePayslipsJob
{
    public Task Execute(Guid payrollRunId) => Task.CompletedTask;
}
