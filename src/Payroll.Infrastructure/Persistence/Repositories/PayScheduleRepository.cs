using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayScheduleRepository(PayrollDbContext db) : IPayScheduleRepository
{
    public Task<PaySchedule?> GetAsync(CancellationToken ct = default) =>
        db.PaySchedules.FirstOrDefaultAsync(ct);

    public async Task AddAsync(PaySchedule schedule, CancellationToken ct = default) =>
        await db.PaySchedules.AddAsync(schedule, ct);

    public void Update(PaySchedule schedule) =>
        db.PaySchedules.Update(schedule);
}
