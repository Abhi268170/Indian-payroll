using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class EmployeeVehicleDetail : AuditableEntity
{
    private EmployeeVehicleDetail() { }

    public Guid EmployeeId { get; private set; }
    // Owner is always Employer in v1; Employee-owned not supported
    public bool MaintainedByEmployer { get; private set; }
    public bool CubicCapacityAbove1600 { get; private set; }
    public bool DriverProvided { get; private set; }

    public static EmployeeVehicleDetail Create(
        Guid employeeId,
        bool maintainedByEmployer,
        bool cubicCapacityAbove1600,
        bool driverProvided,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            MaintainedByEmployer = maintainedByEmployer,
            CubicCapacityAbove1600 = cubicCapacityAbove1600,
            DriverProvided = driverProvided,
            CreatedBy = createdBy
        };

    public void Update(
        bool maintainedByEmployer,
        bool cubicCapacityAbove1600,
        bool driverProvided,
        Guid updatedBy)
    {
        MaintainedByEmployer = maintainedByEmployer;
        CubicCapacityAbove1600 = cubicCapacityAbove1600;
        DriverProvided = driverProvided;
        SetUpdated(updatedBy);
    }
}
