using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface ISalaryRevisionRepository
{
    Task AddAsync(SalaryRevision revision, CancellationToken ct = default);
    Task<SalaryRevision?> GetByIdWithOverridesAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SalaryRevision>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    // Applied revisions whose payout falls in the given period and whose arrears
    // have not yet been paid out (ArrearPaidRunId is null). Used by payout-run
    // arrear injection (WI-018 step 5).
    Task<IReadOnlyList<SalaryRevision>> GetAppliedUnpaidForPayoutAsync(
        int payoutYear, int payoutMonth, CancellationToken ct = default);

    // Revisions whose arrears were paid out by a specific run — used to reverse the
    // paid flag if that run's approval is later rejected.
    Task<IReadOnlyList<SalaryRevision>> GetByArrearPaidRunAsync(Guid runId, CancellationToken ct = default);

    void Update(SalaryRevision revision);
}
