namespace Payroll.Engine.Outputs;

public sealed record PayrollResult(
    Guid EmployeeId,
    GrossResult Gross,
    TDSResult TDS,
    PFResult PF,
    ESIResult ESI,
    PTResult PT,
    LWFResult LWF,
    decimal NetPay,
    GratuityResult Gratuity);
