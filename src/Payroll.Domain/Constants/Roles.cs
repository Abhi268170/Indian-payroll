namespace Payroll.Domain.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrgAdmin = "OrgAdmin";
    public const string HRManager = "HRManager";
    public const string PayrollManager = "PayrollManager";
    public const string FinanceViewer = "FinanceViewer";
    public const string Employee = "Employee";

    public static readonly string[] All =
    [
        SuperAdmin, OrgAdmin, HRManager, PayrollManager, FinanceViewer, Employee
    ];
}
