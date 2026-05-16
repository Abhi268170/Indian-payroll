using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class OrgProfileRepository(PayrollDbContext db) : IOrgProfileRepository
{
    public Task<OrgProfile?> GetAsync(CancellationToken ct = default) =>
        db.OrgProfiles.FirstOrDefaultAsync(ct);

    public async Task AddAsync(OrgProfile profile, CancellationToken ct = default) =>
        await db.OrgProfiles.AddAsync(profile, ct);

    public void Update(OrgProfile profile) =>
        db.OrgProfiles.Update(profile);
}
