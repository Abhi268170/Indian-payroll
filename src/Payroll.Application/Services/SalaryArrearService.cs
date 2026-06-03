using System.Text.Json;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Services;

public sealed record ArrearMonthExclusion(int Year, int Month, string Reason);

public sealed record SalaryArrearResult(
    IReadOnlyList<ArrearLine> Lines,
    decimal TotalArrear,
    decimal TotalTaxableArrear,
    IReadOnlyList<ArrearMonthExclusion> ExcludedMonths);

public interface ISalaryArrearService
{
    // Computes arrears owed for a revision by diffing each finalised back month
    // (EffectiveFrom .. Payout-1) against the revision's new structure. Pure math in
    // SalaryArrearCalculator; this shell loads the runs, breakdowns, and frozen config.
    Task<SalaryArrearResult> ComputeForRevisionAsync(Guid revisionId, CancellationToken ct = default);
}

public sealed class SalaryArrearService(
    ISalaryRevisionRepository revisionRepo,
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository salaryComponentRepo)
    : ISalaryArrearService
{
    public async Task<SalaryArrearResult> ComputeForRevisionAsync(Guid revisionId, CancellationToken ct = default)
    {
        SalaryRevision revision = await revisionRepo.GetByIdWithOverridesAsync(revisionId, ct)
            ?? throw new NotFoundException($"Salary revision {revisionId} not found.");

        // Per-component arrears need a resolvable structure. BuildComponentInputs requires
        // a template; an override-only revision cannot be resolved into component amounts.
        if (revision.SalaryStructureTemplateId is null)
            throw new DomainException("Arrear computation requires a salary structure template on the revision.");

        SalaryStructureTemplate template =
            await templateRepo.GetByIdWithComponentsAsync(revision.SalaryStructureTemplateId.Value, ct)
            ?? throw new DomainException("Salary structure template for the revision was not found.");

        EmployeeSalaryStructure newStructure = BuildInMemoryStructure(revision);
        Dictionary<Guid, SalaryComponent> addedCompDetails = await LoadAddedComponentsAsync(revision, template, ct);

        int startIdx = revision.EffectiveFromYear * 12 + (revision.EffectiveFromMonth - 1);
        int payoutIdx = revision.PayoutYear * 12 + (revision.PayoutMonth - 1);

        List<ArrearMonth> months = new List<ArrearMonth>();
        List<ArrearMonthExclusion> excluded = new List<ArrearMonthExclusion>();

        for (int idx = startIdx; idx < payoutIdx; idx++)
        {
            int year = idx / 12;
            int month = idx % 12 + 1;

            PayrollRun? run = await runRepo.GetFinalisedRegularRunForPeriodAsync(year, month, ct);
            if (run is null)
            {
                excluded.Add(new ArrearMonthExclusion(year, month, "No finalised regular run for this month."));
                continue;
            }

            PayrunEmployee? pe = await payrunEmpRepo.GetByRunAndEmployeeAsync(run.Id, revision.EmployeeId, ct);
            if (pe is null || pe.Status != PayrunEmployeeStatus.Active)
            {
                excluded.Add(new ArrearMonthExclusion(year, month, "Employee was not actively paid in that run."));
                continue;
            }

            if (run.StatutoryConfigSnapshot is null)
            {
                excluded.Add(new ArrearMonthExclusion(year, month, "Run is missing its statutory config snapshot."));
                continue;
            }

            StatutoryConfig config = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

            IReadOnlyList<PayrunComponentBreakdown> breakdowns =
                await breakdownRepo.GetByRunAndEmployeeAsync(run.Id, revision.EmployeeId, ct);

            // Old prorated amounts: recurring salary-structure earnings only — exclude
            // reimbursements, one-time earnings, and employer benefits (none of which
            // a structure revision changes).
            List<ArrearOldComponent> old = breakdowns
                .Where(b => b.SalaryComponentId is not null
                    && !b.IsOneTimeEarning
                    && !b.IsBenefit
                    && !string.Equals(b.ComponentCode, "REIMBURSEMENT", StringComparison.OrdinalIgnoreCase))
                .Select(b => new ArrearOldComponent(b.ComponentCode, b.ProratedAmount, b.FullAmount))
                .ToList();

            IReadOnlyList<SalaryComponentInput> newInputs =
                InitiatePayrollRunHandler.BuildComponentInputs(newStructure, template, addedCompDetails, config);

            List<ArrearNewComponent> newComps = newInputs
                .Select(c => new ArrearNewComponent(c.ComponentId, c.Code, c.Code, c.Amount, c.IsTaxable))
                .ToList();

            months.Add(new ArrearMonth(year, month, old, newComps));
        }

        ArrearComputation comp = SalaryArrearCalculator.Compute(months);
        return new SalaryArrearResult(comp.Lines, comp.TotalArrear, comp.TotalTaxableArrear, excluded);
    }

    private static EmployeeSalaryStructure BuildInMemoryStructure(SalaryRevision revision)
    {
        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            revision.EmployeeId,
            revision.TenantId,
            revision.SalaryStructureTemplateId,
            revision.NewAnnualCTC,
            new DateOnly(revision.EffectiveFromYear, revision.EffectiveFromMonth, 1),
            revision.CreatedBy);

        foreach (SalaryRevisionComponentOverride o in revision.ComponentOverrides)
        {
            structure.AddOverride(EmployeeSalaryComponentOverride.Create(
                structure.Id, o.SalaryComponentId, o.FormulaType, o.Percentage, o.FixedAmount, revision.CreatedBy));
        }

        return structure;
    }

    private async Task<Dictionary<Guid, SalaryComponent>> LoadAddedComponentsAsync(
        SalaryRevision revision, SalaryStructureTemplate template, CancellationToken ct)
    {
        HashSet<Guid> templateCompIds = template.Components.Select(c => c.ComponentId).ToHashSet();
        List<Guid> addedIds = revision.ComponentOverrides
            .Select(o => o.SalaryComponentId)
            .Where(id => !templateCompIds.Contains(id))
            .Distinct()
            .ToList();

        if (addedIds.Count == 0) return [];
        return (await salaryComponentRepo.GetByIdsAsync(addedIds, ct)).ToDictionary(c => c.Id);
    }
}
