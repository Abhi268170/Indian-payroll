using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record PayRunTaxLineDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    decimal AnnualProjectedIncome,
    decimal StandardDeduction,
    decimal TaxableIncome,
    decimal TaxBeforeRebate,
    decimal Rebate87A,
    decimal Surcharge,
    decimal Cess,
    decimal AnnualTaxLiability,
    decimal TdsThisMonth,
    bool HasPanOverride,
    decimal? TdsOverrideAmount = null,
    decimal EffectiveTds = 0m);

public record GetPayRunTaxesQuery(Guid RunId) : IRequest<IReadOnlyList<PayRunTaxLineDto>>;

public sealed class GetPayRunTaxesHandler(
    IPayrollRunRepository runRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo)
    : IRequestHandler<GetPayRunTaxesQuery, IReadOnlyList<PayRunTaxLineDto>>
{
    public async Task<IReadOnlyList<PayRunTaxLineDto>> Handle(GetPayRunTaxesQuery req, CancellationToken ct)
    {
        _ = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        IReadOnlyList<Domain.Entities.TdsWorksheet> worksheets = await tdsWorksheetRepo.GetByRunIdAsync(req.RunId, ct);
        IReadOnlyList<Domain.Entities.PayrunEmployee> payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        Dictionary<Guid, Domain.Entities.PayrunEmployee> peMap = payrunEmps.ToDictionary(p => p.EmployeeId);
        IReadOnlyList<Domain.Entities.Employee> employees = await employeeRepo.GetManyByIdsAsync(
            worksheets.Select(w => w.EmployeeId), ct);
        Dictionary<Guid, Domain.Entities.Employee> employeeMap = employees.ToDictionary(e => e.Id);

        List<PayRunTaxLineDto> result = new List<PayRunTaxLineDto>(worksheets.Count);
        foreach (Domain.Entities.TdsWorksheet ws in worksheets)
        {
            if (!employeeMap.TryGetValue(ws.EmployeeId, out Domain.Entities.Employee? emp)) continue;

            result.Add(new PayRunTaxLineDto(
                EmployeeId: ws.EmployeeId,
                EmployeeCode: emp.EmployeeCode,
                EmployeeName: emp.FullName,
                AnnualProjectedIncome: ws.AnnualProjectedIncome,
                StandardDeduction: ws.StandardDeduction,
                TaxableIncome: ws.TaxableIncome,
                TaxBeforeRebate: ws.TaxBeforeRebate,
                Rebate87A: ws.Rebate87A,
                Surcharge: ws.Surcharge,
                Cess: ws.Cess,
                AnnualTaxLiability: ws.AnnualTaxLiability,
                TdsThisMonth: ws.TdsThisMonth,
                HasPanOverride: ws.HasPanOverride,
                TdsOverrideAmount: peMap.GetValueOrDefault(ws.EmployeeId)?.TdsOverrideAmount,
                EffectiveTds: peMap.GetValueOrDefault(ws.EmployeeId)?.TdsOverrideAmount ?? ws.TdsThisMonth));
        }

        result.Sort((a, b) => string.Compare(a.EmployeeCode, b.EmployeeCode, StringComparison.Ordinal));
        return result;
    }
}
