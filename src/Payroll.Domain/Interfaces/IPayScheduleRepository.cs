using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IPayScheduleRepository
{
    Task<PaySchedule?> GetAsync(CancellationToken ct = default);
    Task AddAsync(PaySchedule schedule, CancellationToken ct = default);
    void Update(PaySchedule schedule);
}
