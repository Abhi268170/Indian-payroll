using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateEmployeeHandler(
    IEmployeeRepository employees,
    ITenantContext tenantContext,
    IEncryptionService encryption,
    IUnitOfWork uow) : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        string encryptedPAN = encryption.Encrypt(request.PAN);
        Employee employee = Employee.Create(
            request.FirstName,
            request.LastName,
            request.EmployeeCode,
            encryptedPAN,
            request.DateOfBirth,
            request.Gender,
            tenantContext.TenantId,
            request.DepartmentId,
            request.DesignationId,
            request.DateOfJoining,
            request.WorkState,
            request.EmploymentType,
            request.ActorId,
            request.BranchId,
            request.CostCentreId);

        await employees.AddAsync(employee, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return employee.Id;
    }
}
