using System.Text.RegularExpressions;
using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record ValidateEmployeeImportCommand(
    Stream CsvFile,
    bool OverwriteExisting,
    Guid ActorId) : IRequest<ImportValidationResult>;

public sealed class ValidateEmployeeImportHandler(
    IEmployeeImportParser parser,
    IEmployeeRepository employeeRepo,
    IDepartmentRepository departmentRepo,
    IDesignationRepository designationRepo,
    IWorkLocationRepository workLocationRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ITenantContext tenantContext)
    : IRequestHandler<ValidateEmployeeImportCommand, ImportValidationResult>
{
    private static readonly Regex PanRegex = new(@"^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled);
    private static readonly Regex AadhaarRegex = new(@"^\d{12}$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^\d{10}$", RegexOptions.Compiled);

    public async Task<ImportValidationResult> Handle(ValidateEmployeeImportCommand req, CancellationToken ct)
    {
        IReadOnlyList<EmployeeImportRow> rows = parser.Parse(req.CsvFile);

        if (rows.Count > 1000)
            throw new DomainException("Import file exceeds the 1000-row limit. Split it into smaller files.");

        Dictionary<string, Guid> departments = (await departmentRepo.ListAsync(ct))
            .ToDictionary(d => d.Name, d => d.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Guid> designations = (await designationRepo.ListAsync(ct))
            .ToDictionary(d => d.Name, d => d.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Guid> locations = (await workLocationRepo.ListAsync(ct))
            .ToDictionary(d => d.Name, d => d.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Guid> templates = (await templateRepo.ListByTenantAsync(tenantContext.TenantId, ct))
            .ToDictionary(t => t.Name, t => t.Id, StringComparer.OrdinalIgnoreCase);

        IEnumerable<string> csvCodes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeNumber))
            .Select(r => r.EmployeeNumber!);
        IEnumerable<string> csvEmails = rows.Select(r => r.WorkEmail);

        HashSet<string> existingCodes = await employeeRepo.GetExistingCodesAsync(csvCodes, ct);
        HashSet<string> existingEmails = await employeeRepo.GetExistingEmailsAsync(csvEmails, ct);

        List<ImportRowError> errors = [];
        List<EmployeeImportRow> newRows = [];
        List<EmployeeImportRow> updateRows = [];
        List<EmployeeImportRow> skippedRows = [];

        foreach (EmployeeImportRow row in rows)
        {
            List<string> rowErrors = ValidateRow(row, departments, designations, locations, templates);
            if (rowErrors.Count > 0)
            {
                foreach (string msg in rowErrors)
                    errors.Add(new ImportRowError(row.RowNumber, row.EmployeeNumber, msg));
                continue;
            }

            bool isDuplicate = (!string.IsNullOrWhiteSpace(row.EmployeeNumber) && existingCodes.Contains(row.EmployeeNumber!))
                || existingEmails.Contains(row.WorkEmail);

            if (isDuplicate)
            {
                if (req.OverwriteExisting)
                    updateRows.Add(row);
                else
                    skippedRows.Add(row);
            }
            else
            {
                newRows.Add(row);
            }
        }

        return new ImportValidationResult(newRows, updateRows, skippedRows, errors);
    }

    private static List<string> ValidateRow(
        EmployeeImportRow row,
        Dictionary<string, Guid> departments,
        Dictionary<string, Guid> designations,
        Dictionary<string, Guid> locations,
        Dictionary<string, Guid> templates)
    {
        List<string> errs = [];

        if (string.IsNullOrWhiteSpace(row.FirstName)) errs.Add("FirstName is required.");
        if (string.IsNullOrWhiteSpace(row.LastName)) errs.Add("LastName is required.");
        // FathersName is required because InitiatePayrollRunCommand:194 skips any employee
        // whose father's name is blank. Same rule as UpdatePersonalDetailsValidator.
        if (string.IsNullOrWhiteSpace(row.FathersName)) errs.Add("FathersName is required.");

        if (string.IsNullOrWhiteSpace(row.WorkEmail))
            errs.Add("WorkEmail is required.");
        else if (!row.WorkEmail.Contains('@'))
            errs.Add($"WorkEmail '{row.WorkEmail}' is not a valid email address.");

        if (string.IsNullOrWhiteSpace(row.DateOfJoining) || !DateOnly.TryParse(row.DateOfJoining, out _))
            errs.Add("DateOfJoining must be a valid date (YYYY-MM-DD).");
        if (string.IsNullOrWhiteSpace(row.DateOfBirth) || !DateOnly.TryParse(row.DateOfBirth, out _))
            errs.Add("DateOfBirth must be a valid date (YYYY-MM-DD).");

        if (!Enum.TryParse<Gender>(row.Gender, ignoreCase: true, out _))
            errs.Add($"Gender '{row.Gender}' is invalid. Use Male, Female, or Other.");
        if (!Enum.TryParse<EmploymentType>(row.EmploymentType, ignoreCase: true, out _))
            errs.Add($"EmploymentType '{row.EmploymentType}' is invalid.");

        if (row.PAN is not null && !PanRegex.IsMatch(row.PAN))
            errs.Add("PAN must be in format AAAAA9999A.");
        if (row.Aadhaar is not null && !AadhaarRegex.IsMatch(row.Aadhaar))
            errs.Add("Aadhaar must be 12 digits.");
        if (row.MobileNumber is not null && !MobileRegex.IsMatch(row.MobileNumber))
            errs.Add("MobileNumber must be 10 digits.");
        if (row.IFSCCode is not null && row.IFSCCode.Length != 11)
            errs.Add("IFSCCode must be 11 characters.");

        if (string.IsNullOrWhiteSpace(row.Department))
            errs.Add("Department is required.");
        else if (!departments.ContainsKey(row.Department))
            errs.Add($"Department '{row.Department}' not found — create it in Settings first.");

        if (string.IsNullOrWhiteSpace(row.Designation))
            errs.Add("Designation is required.");
        else if (!designations.ContainsKey(row.Designation))
            errs.Add($"Designation '{row.Designation}' not found — create it in Settings first.");

        if (string.IsNullOrWhiteSpace(row.WorkLocation))
            errs.Add("WorkLocation is required.");
        else if (!locations.ContainsKey(row.WorkLocation))
            errs.Add($"WorkLocation '{row.WorkLocation}' not found — create it in Settings first.");

        if (row.SalaryStructureTemplate is not null && !templates.ContainsKey(row.SalaryStructureTemplate))
            errs.Add($"Salary structure template '{row.SalaryStructureTemplate}' not found.");

        if (row.AnnualCTC is not null && (!decimal.TryParse(row.AnnualCTC, out decimal ctc) || ctc <= 0))
            errs.Add("AnnualCTC must be a positive number.");

        // PaymentMode is mandatory in the import. Without it, CommitEmployeeImportCommand
        // short-circuits ApplyPaymentInfo and leaves the row with no bank account, which the
        // engine then silently skips at payroll time (InitiatePayrollRunCommand:195). Force
        // a choice at validation time.
        if (string.IsNullOrWhiteSpace(row.PaymentMode))
            errs.Add("PaymentMode is required.");
        else if (!Enum.TryParse<PaymentMode>(row.PaymentMode, ignoreCase: true, out _))
            errs.Add($"PaymentMode '{row.PaymentMode}' is invalid.");

        // Bank-field conditional requirement — mirrors UpdatePaymentInfoCommand.cs:21 which
        // treats BOTH BankTransfer AND DirectDeposit as bank-mandatory modes. Aligned with
        // the engine's per-employee skip in InitiatePayrollRunCommand:195 ("Bank account
        // missing").
        if (Enum.TryParse<PaymentMode>(row.PaymentMode, ignoreCase: true, out PaymentMode mode)
            && (mode == PaymentMode.BankTransfer || mode == PaymentMode.DirectDeposit))
        {
            string modeName = mode.ToString();
            if (string.IsNullOrWhiteSpace(row.BankAccountNumber))
                errs.Add($"BankAccountNumber is required when PaymentMode is {modeName}.");
            if (string.IsNullOrWhiteSpace(row.BankAccountHolderName))
                errs.Add($"BankAccountHolderName is required when PaymentMode is {modeName}.");
            if (string.IsNullOrWhiteSpace(row.BankName))
                errs.Add($"BankName is required when PaymentMode is {modeName}.");
            if (string.IsNullOrWhiteSpace(row.IFSCCode))
                errs.Add($"IFSCCode is required when PaymentMode is {modeName}.");
            if (string.IsNullOrWhiteSpace(row.BankAccountType))
                errs.Add($"BankAccountType is required when PaymentMode is {modeName}.");
        }

        if (row.State is not null && !Enum.TryParse<IndianState>(row.State, ignoreCase: true, out _))
            errs.Add($"State '{row.State}' is not a recognised Indian state code.");

        if (row.BankAccountType is not null && !Enum.TryParse<AccountType>(row.BankAccountType, ignoreCase: true, out _))
            errs.Add($"BankAccountType '{row.BankAccountType}' is invalid.");

        return errs;
    }
}
