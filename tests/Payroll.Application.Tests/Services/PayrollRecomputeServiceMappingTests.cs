using FluentAssertions;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Xunit;

namespace Payroll.Application.Tests.Services;

// Regression tests for the two bugs the prior audit found in
// SetLopCommandHandler.RecomputeEmployee:
//   - IsTaxable was hardcoded to true, silently making non-taxable allowances
//     (medical, reimbursement-style components) taxable after any LOP edit.
//   - ConsiderForEsi was dropped from the SalaryComponentInput mapping, so ESI
//     base shrank to Basic + EPF components on recompute.
//
// These tests pin the canonical mapping so the same bugs cannot regress.
public class PayrollRecomputeServiceMappingTests
{
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CompId = Guid.NewGuid();

    private static PayrunComponentBreakdown Breakdown(
        string code,
        decimal amount,
        bool isTaxable,
        bool considerForEpf = false,
        bool considerForEsi = false,
        bool calculateOnProRata = true,
        bool isOneTimeEarning = false,
        Guid? salaryComponentId = null) =>
        PayrunComponentBreakdown.Create(
            payrollRunId: RunId,
            employeeId: EmpId,
            tenantId: TenantId,
            salaryComponentId: salaryComponentId ?? CompId,
            componentCode: code,
            componentName: code,
            fullAmount: amount,
            proratedAmount: amount,
            isOneTimeEarning: isOneTimeEarning,
            isTaxable: isTaxable,
            considerForEpf: considerForEpf,
            considerForEsi: considerForEsi,
            calculateOnProRata: calculateOnProRata,
            epfInclusionRule: EpfInclusionRule.Always,
            showInPayslip: true);

    [Fact]
    public void MapToEngineInput_NonTaxableComponent_PreservesIsTaxableFalse()
    {
        var bd = Breakdown("MEDICAL", 1_500m, isTaxable: false);

        var input = PayrollRecomputeService.MapToEngineInput(bd);

        input.IsTaxable.Should().BeFalse();
    }

    [Fact]
    public void MapToEngineInput_EsiApplicableComponent_PreservesConsiderForEsiTrue()
    {
        var bd = Breakdown("HRA", 8_000m, isTaxable: true, considerForEsi: true);

        var input = PayrollRecomputeService.MapToEngineInput(bd);

        input.ConsiderForEsi.Should().BeTrue();
    }

    [Fact]
    public void MapToEngineInput_OneTimeBonus_OverridesProRataToFalse()
    {
        // Component admin may have flagged Bonus as proratable; one-time entries
        // are always paid flat per Zoho rule. Service forces it false.
        var bd = Breakdown("BONUS", 10_000m, isTaxable: true,
            calculateOnProRata: true, isOneTimeEarning: true);

        var input = PayrollRecomputeService.MapToEngineInput(bd);

        input.CalculateOnProRata.Should().BeFalse();
    }

    [Fact]
    public void MapToEngineInput_RecurringComponent_KeepsProRataFlag()
    {
        var bd = Breakdown("BASICSALARY", 25_000m, isTaxable: true,
            calculateOnProRata: true, isOneTimeEarning: false);

        var input = PayrollRecomputeService.MapToEngineInput(bd);

        input.CalculateOnProRata.Should().BeTrue();
    }

    [Fact]
    public void IsReimbursement_NullSalaryComponentId_ReturnsTrue()
    {
        var bd = Breakdown("ANYTHING", 500m, isTaxable: false,
            salaryComponentId: null);
        // Override to nullable null since helper assigns CompId by default.
        // Use raw factory to keep null.
        bd = PayrunComponentBreakdown.Create(
            payrollRunId: RunId, employeeId: EmpId, tenantId: TenantId,
            salaryComponentId: null, componentCode: "ANYTHING", componentName: "x",
            fullAmount: 500m, proratedAmount: 500m, isOneTimeEarning: true);

        PayrollRecomputeService.IsReimbursement(bd).Should().BeTrue();
    }

    [Fact]
    public void IsReimbursement_ReimbursementCode_ReturnsTrue()
    {
        var bd = PayrunComponentBreakdown.Create(
            payrollRunId: RunId, employeeId: EmpId, tenantId: TenantId,
            salaryComponentId: CompId, componentCode: "REIMBURSEMENT", componentName: "Travel",
            fullAmount: 1_200m, proratedAmount: 1_200m, isOneTimeEarning: true);

        PayrollRecomputeService.IsReimbursement(bd).Should().BeTrue();
    }

    [Fact]
    public void IsReimbursement_RegularEarning_ReturnsFalse()
    {
        var bd = Breakdown("BASICSALARY", 25_000m, isTaxable: true);

        PayrollRecomputeService.IsReimbursement(bd).Should().BeFalse();
    }
}
