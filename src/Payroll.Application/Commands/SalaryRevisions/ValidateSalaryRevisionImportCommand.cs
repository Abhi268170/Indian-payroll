using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.SalaryRevisions;

public record ValidateSalaryRevisionImportCommand(Stream File, bool OverwriteExisting)
    : IRequest<SalaryRevisionImportValidationResult>;

// A row whose fields parsed + resolved successfully, ready to create on commit.
internal sealed record ResolvedRevisionRow(
    SalaryRevisionImportRow Raw,
    Guid EmployeeId,
    decimal PreviousAnnualCTC,
    decimal NewAnnualCTC,
    int EffectiveFromMonth,
    int EffectiveFromYear,
    int PayoutMonth,
    int PayoutYear,
    Guid TemplateId,
    string? Notes,
    bool IsDuplicate);

// Shared parse + resolve + validate logic for both validate and commit, so the preview
// and the persisted result can never diverge.
internal static class SalaryRevisionImportProcessor
{
    public const int MaxRows = 1000;

    public static (List<ResolvedRevisionRow> Valid,
                   List<SalaryRevisionImportRow> Skipped,
                   List<SalaryRevisionImportRowError> Errors) Process(
        IReadOnlyList<SalaryRevisionImportRow> rows,
        IReadOnlyDictionary<string, Employee> employeesByCode,
        IReadOnlyDictionary<string, Guid> templatesByName,
        IReadOnlyDictionary<Guid, decimal> activeCtcByEmployee,
        IReadOnlyDictionary<Guid, HashSet<(int Month, int Year)>> existingPayoutsByEmployee,
        bool overwrite)
    {
        List<ResolvedRevisionRow> valid = new List<ResolvedRevisionRow>();
        List<SalaryRevisionImportRow> skipped = new List<SalaryRevisionImportRow>();
        List<SalaryRevisionImportRowError> errors = new List<SalaryRevisionImportRowError>();

        foreach (SalaryRevisionImportRow row in rows)
        {
            void Error(string msg) => errors.Add(new SalaryRevisionImportRowError(row.RowNumber, row.EmployeeNumber, msg));

            if (string.IsNullOrWhiteSpace(row.EmployeeNumber))
            { Error("EmployeeNumber is required."); continue; }
            if (!employeesByCode.TryGetValue(row.EmployeeNumber, out Employee? employee))
            { Error($"Employee '{row.EmployeeNumber}' not found in this tenant."); continue; }

            if (!decimal.TryParse(row.NewAnnualCTC, out decimal newCtc) || newCtc <= 0m)
            { Error("NewAnnualCTC must be a number greater than zero."); continue; }
            if (!TryMonth(row.EffectiveFromMonth, out int effMonth))
            { Error("EffectiveFromMonth must be 1-12."); continue; }
            if (!TryYear(row.EffectiveFromYear, out int effYear))
            { Error("EffectiveFromYear must be a valid year."); continue; }
            if (!TryMonth(row.PayoutMonth, out int payoutMonth))
            { Error("PayoutMonth must be 1-12."); continue; }
            if (!TryYear(row.PayoutYear, out int payoutYear))
            { Error("PayoutYear must be a valid year."); continue; }
            if (effYear * 12 + effMonth > payoutYear * 12 + payoutMonth)
            { Error("Effective-from month must be on or before the payout month."); continue; }

            if (string.IsNullOrWhiteSpace(row.SalaryStructureTemplate)
                || !templatesByName.TryGetValue(row.SalaryStructureTemplate, out Guid templateId))
            { Error($"SalaryStructureTemplate '{row.SalaryStructureTemplate}' not found in this tenant."); continue; }

            if (!activeCtcByEmployee.TryGetValue(employee.Id, out decimal previousCtc))
            { Error("Employee has no active salary structure to revise."); continue; }

            bool duplicate = existingPayoutsByEmployee.TryGetValue(employee.Id, out HashSet<(int, int)>? periods)
                && periods.Contains((payoutMonth, payoutYear));
            if (duplicate && !overwrite)
            { skipped.Add(row); continue; }

            valid.Add(new ResolvedRevisionRow(
                row, employee.Id, previousCtc, newCtc, effMonth, effYear,
                payoutMonth, payoutYear, templateId, row.Notes, duplicate));
        }

        return (valid, skipped, errors);
    }

    private static bool TryMonth(string? s, out int m) => int.TryParse(s, out m) && m is >= 1 and <= 12;
    private static bool TryYear(string? s, out int y) => int.TryParse(s, out y) && y is >= 2000 and <= 2100;
}

public sealed class ValidateSalaryRevisionImportHandler(
    ISalaryRevisionImportParser parser,
    IEmployeeRepository employeeRepo,
    ISalaryStructureTemplateRepository templateRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo,
    ISalaryRevisionRepository revisionRepo,
    ITenantContext tenantContext)
    : IRequestHandler<ValidateSalaryRevisionImportCommand, SalaryRevisionImportValidationResult>
{
    public async Task<SalaryRevisionImportValidationResult> Handle(
        ValidateSalaryRevisionImportCommand req, CancellationToken ct)
    {
        IReadOnlyList<SalaryRevisionImportRow> rows = parser.Parse(req.File);
        if (rows.Count > SalaryRevisionImportProcessor.MaxRows)
            throw new DomainException($"Import file exceeds the {SalaryRevisionImportProcessor.MaxRows}-row limit.");

        SalaryRevisionImportContext ctx = await SalaryRevisionImportContext.LoadAsync(
            rows, employeeRepo, templateRepo, salaryStructureRepo, revisionRepo, tenantContext, ct);

        (List<ResolvedRevisionRow> valid, List<SalaryRevisionImportRow> skipped, List<SalaryRevisionImportRowError> errors) = SalaryRevisionImportProcessor.Process(
            rows, ctx.EmployeesByCode, ctx.TemplatesByName, ctx.ActiveCtcByEmployee,
            ctx.ExistingPayoutsByEmployee, req.OverwriteExisting);

        return new SalaryRevisionImportValidationResult(
            valid.Select(v => v.Raw).ToList(), skipped, errors);
    }
}
