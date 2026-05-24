using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IOrgProfileRepository
{
    Task<OrgProfile?> GetAsync(CancellationToken ct = default);
    Task AddAsync(OrgProfile profile, CancellationToken ct = default);
    void Update(OrgProfile profile);
}
