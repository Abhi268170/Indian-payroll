namespace Payroll.Domain.Common;

public sealed class PayrollRunHasBlockingTasksException(int hardBlockCount)
    : Exception($"Payroll run has {hardBlockCount} blocking task(s) that must be resolved before approval.");
