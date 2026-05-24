using System.Text.RegularExpressions;
using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record CommitEmployeeImportCommand(
    Stream CsvFile,
    bool OverwriteExisting,
    Guid ActorId) : IRequest<CommitImportResult>;

public record CommitImportResult(int Created, int Updated, int Skipped);

public sealed class CommitEmployeeImportHandler(
    IEmployeeImportParser parser,
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryRepo,
    IDepartmentRepository departmentRepo,
    IDesignationRepository designationRepo,
    IWorkLocationRepository workLocationRepo,
    ISalaryStructureTemplateRepository templateRepo,
    IEncryptionService enc,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CommitEmployeeImportCommand, CommitImportResult>
{
    private static readonly Regex PanRegex = new(@"^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled);

    public async Task<CommitImportResult> Handle(CommitEmployeeImportCommand req, CancellationToken ct)
    {
        IReadOnlyList<EmployeeImportRow> rows = parser.Parse(req.CsvFile);

        if (rows.Count > 1000)
            throw new DomainException("Import file exceeds the 1000-row limit.");

        // Load all reference data
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

        // Separate rows into new / update / skip
        List<EmployeeImportRow> newRows = [];
        List<EmployeeImportRow> updateRows = [];
        int skipped = 0;

        foreach (EmployeeImportRow row in rows)
        {
            bool isDuplicate = (!string.IsNullOrWhiteSpace(row.EmployeeNumber) && existingCodes.Contains(row.EmployeeNumber!))
                || existingEmails.Contains(row.WorkEmail);

            if (isDuplicate)
            {
                if (req.OverwriteExisting)
                    updateRows.Add(row);
                else
                    skipped++;
            }
            else
            {
                newRows.Add(row);
            }
        }

        // Batch-load employees we need to update
        Dictionary<string, Employee> byCode = [];
        Dictionary<string, Employee> byEmail = [];

        if (updateRows.Count > 0)
        {
            IEnumerable<string> updateCodes = updateRows
                .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeNumber))
                .Select(r => r.EmployeeNumber!);
            IReadOnlyList<Employee> byCodeList = await employeeRepo.GetManyByCodesAsync(updateCodes, ct);
            byCode = byCodeList.ToDictionary(e => e.EmployeeCode, StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> updateEmails = updateRows
                .Where(r => string.IsNullOrWhiteSpace(r.EmployeeNumber) || !byCode.ContainsKey(r.EmployeeNumber!))
                .Select(r => r.WorkEmail);
            IReadOnlyList<Employee> byEmailList = await employeeRepo.GetManyByEmailsAsync(updateEmails, ct);
            byEmail = byEmailList.ToDictionary(e => e.WorkEmail, StringComparer.OrdinalIgnoreCase);
        }

        int nextCodeSeed = (await employeeRepo.ListAsync(ct)).Count;

        // Create new employees
        foreach (EmployeeImportRow row in newRows)
        {
            string code = !string.IsNullOrWhiteSpace(row.EmployeeNumber)
                ? row.EmployeeNumber!.Trim().ToUpperInvariant()
                : $"EMP{(++nextCodeSeed):D3}";

            Employee emp = Employee.CreateStep1(
                row.FirstName,
                row.MiddleName,
                row.LastName,
                code,
                row.WorkEmail,
                row.MobileNumber,
                Enum.Parse<Gender>(row.Gender, ignoreCase: true),
                DateOnly.Parse(row.DateOfJoining),
                Enum.Parse<EmploymentType>(row.EmploymentType, ignoreCase: true),
                isDirector: false,
                enablePortalAccess: false,
                tenantContext.TenantId,
                departments[row.Department],
                designations[row.Designation],
                locations[row.WorkLocation],
                businessUnitId: null,
                DateOnly.Parse(row.DateOfBirth),
                req.ActorId);

            ApplyPersonalDetails(emp, row, req.ActorId, existing: null);
            ApplyPaymentInfo(emp, row, req.ActorId, existing: null);
            ApplyStatutoryDetails(emp, row, req.ActorId);

            await employeeRepo.AddAsync(emp, ct);

            bool addedStructure = false;
            if (row.AnnualCTC is not null && decimal.TryParse(row.AnnualCTC, out decimal ctc))
            {
                Guid? templateId = row.SalaryStructureTemplate is not null && templates.TryGetValue(row.SalaryStructureTemplate, out Guid tid)
                    ? tid
                    : null;
                EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
                    emp.Id, tenantContext.TenantId, templateId, ctc,
                    DateOnly.FromDateTime(DateTime.UtcNow), req.ActorId);
                await salaryRepo.AddAsync(structure, ct);
                addedStructure = true;
            }
            // Keep ProfileComplete consistent with the engine's per-employee skip rules.
            // For a brand-new row, structure presence is whatever this import attached.
            emp.RecomputeProfileComplete(hasActiveSalaryStructure: addedStructure, req.ActorId);
        }

        // Update existing employees (merge semantics: blank CSV cell = keep DB value)
        foreach (EmployeeImportRow row in updateRows)
        {
            Employee? existing = (!string.IsNullOrWhiteSpace(row.EmployeeNumber) && byCode.TryGetValue(row.EmployeeNumber!, out Employee? byC))
                ? byC
                : byEmail.TryGetValue(row.WorkEmail, out Employee? byE) ? byE : null;

            if (existing is null) continue;

            ApplyPersonalDetails(existing, row, req.ActorId, existing);
            ApplyPaymentInfo(existing, row, req.ActorId, existing);
            ApplyStatutoryDetails(existing, row, req.ActorId);
            employeeRepo.Update(existing);

            bool hasActiveStructure;
            if (row.AnnualCTC is not null && decimal.TryParse(row.AnnualCTC, out decimal ctc))
            {
                EmployeeSalaryStructure? current = await salaryRepo.GetActiveAsync(existing.Id, ct);
                if (current is not null)
                {
                    current.Close(DateOnly.FromDateTime(DateTime.UtcNow), req.ActorId);
                    salaryRepo.Update(current);
                }
                Guid? templateId = row.SalaryStructureTemplate is not null && templates.TryGetValue(row.SalaryStructureTemplate, out Guid tid)
                    ? tid
                    : null;
                EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
                    existing.Id, tenantContext.TenantId, templateId, ctc,
                    DateOnly.FromDateTime(DateTime.UtcNow), req.ActorId);
                await salaryRepo.AddAsync(structure, ct);
                hasActiveStructure = true;
            }
            else
            {
                // No new structure in this row; consult the repo to learn whether the
                // employee still has an active structure from a prior assignment.
                EmployeeSalaryStructure? current = await salaryRepo.GetActiveAsync(existing.Id, ct);
                hasActiveStructure = current is not null;
            }
            // Keep ProfileComplete consistent with the engine's per-employee skip rules.
            existing.RecomputeProfileComplete(hasActiveSalaryStructure: hasActiveStructure, req.ActorId);
        }

        await uow.SaveChangesAsync(ct);

        return new CommitImportResult(newRows.Count, updateRows.Count, skipped);
    }

    private void ApplyPersonalDetails(Employee emp, EmployeeImportRow row, Guid actorId, Employee? existing)
    {
        string? encPan = row.PAN is not null ? enc.Encrypt(row.PAN)
            : existing?.EncryptedPAN;
        string? encAadhaar = row.Aadhaar is not null ? enc.Encrypt(row.Aadhaar)
            : existing?.EncryptedAadhaar;

        IndianState? state = null;
        if (row.State is not null && Enum.TryParse<IndianState>(row.State, ignoreCase: true, out IndianState parsedState))
            state = parsedState;
        else if (existing is not null)
            state = existing.ResidentialState;

        emp.UpdatePersonalDetails(
            emp.DateOfBirth,
            row.FathersName ?? existing?.FathersName,
            encPan,
            encAadhaar,
            row.PersonalEmail ?? existing?.PersonalEmail,
            existing?.DifferentlyAbledType ?? DifferentlyAbledType.None,
            existing?.IsPWD ?? false,
            row.AddressLine1 ?? existing?.AddressLine1,
            row.AddressLine2 ?? existing?.AddressLine2,
            row.City ?? existing?.City,
            state,
            row.PinCode ?? existing?.PinCode,
            actorId);
    }

    private void ApplyPaymentInfo(Employee emp, EmployeeImportRow row, Guid actorId, Employee? existing)
    {
        if (row.PaymentMode is null && existing is null) return;

        PaymentMode mode = row.PaymentMode is not null && Enum.TryParse<PaymentMode>(row.PaymentMode, ignoreCase: true, out PaymentMode pm)
            ? pm
            : existing?.PaymentMode ?? PaymentMode.BankTransfer;

        string? encAcct = row.BankAccountNumber is not null ? enc.Encrypt(row.BankAccountNumber)
            : existing?.EncryptedBankAccount;
        string? encIfsc = row.IFSCCode is not null ? enc.Encrypt(row.IFSCCode)
            : existing?.EncryptedIFSC;

        AccountType? acctType = null;
        if (row.BankAccountType is not null && Enum.TryParse<AccountType>(row.BankAccountType, ignoreCase: true, out AccountType at))
            acctType = at;
        else if (existing is not null)
            acctType = existing.AccountType;

        emp.UpdatePaymentInfo(
            mode,
            row.BankAccountHolderName ?? existing?.AccountHolderName,
            row.BankName ?? existing?.BankName,
            acctType,
            encAcct,
            encIfsc,
            actorId);
    }

    private static void ApplyStatutoryDetails(Employee emp, EmployeeImportRow row, Guid actorId)
    {
        bool epf = ParseBoolOrDefault(row.EpfEnabled, emp.EpfEnabled);
        bool esi = ParseBoolOrDefault(row.EsiEnabled, emp.EsiEnabled);
        bool pt = ParseBoolOrDefault(row.PtEnabled, emp.PtEnabled);
        bool lwf = ParseBoolOrDefault(row.LwfEnabled, emp.LwfEnabled);

        emp.UpdateStatutoryDetails(
            epf, esi, pt, lwf,
            row.UAN ?? emp.UAN,
            row.ESICNumber ?? emp.ESICIPNumber,
            actorId);
    }

    private static bool ParseBoolOrDefault(string? value, bool fallback) =>
        value is null ? fallback : bool.TryParse(value, out bool parsed) ? parsed : fallback;
}
