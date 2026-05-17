using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeQuery(Guid Id) : IRequest<EmployeeDto>;

public sealed class GetEmployeeHandler(
    IEmployeeRepository repo,
    IDepartmentRepository deptRepo,
    IDesignationRepository desigRepo,
    IWorkLocationRepository wlRepo,
    IEncryptionService enc)
    : IRequestHandler<GetEmployeeQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeQuery request, CancellationToken ct)
    {
        Employee employee = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"Employee {request.Id} not found.");

        Department? dept = await deptRepo.GetByIdAsync(employee.DepartmentId, ct);
        Designation? desig = await desigRepo.GetByIdAsync(employee.DesignationId, ct);
        WorkLocation? wl = await wlRepo.GetByIdAsync(employee.WorkLocationId, ct);

        string? maskedAcct = null;
        if (employee.EncryptedBankAccount is not null)
        {
            string plain = enc.Decrypt(employee.EncryptedBankAccount);
            maskedAcct = plain.Length > 4 ? $"XXXX{plain[^4..]}" : "XXXX";
        }

        string? maskedPAN = null;
        if (employee.EncryptedPAN is not null)
        {
            string plain = enc.Decrypt(employee.EncryptedPAN);
            maskedPAN = plain.Length >= 4 ? $"XXXXX{plain[^4..]}" : "XXXXXXXXXX";
        }

        return new EmployeeDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            employee.FullName,
            employee.WorkEmail,
            employee.MobileNumber,
            employee.Gender.ToString(),
            employee.DateOfJoining.ToString("yyyy-MM-dd"),
            employee.DateOfLeaving?.ToString("yyyy-MM-dd"),
            employee.EmploymentType.ToString(),
            employee.Status.ToString(),
            employee.IsDirector,
            employee.EnablePortalAccess,
            employee.ProfileComplete,
            employee.DepartmentId,
            dept?.Name,
            employee.DesignationId,
            desig?.Name,
            employee.WorkLocationId,
            wl?.Name,
            employee.BusinessUnitId,
            employee.CostCentreId,
            employee.DateOfBirth.ToString("yyyy-MM-dd"),
            employee.FathersName,
            employee.PersonalEmail,
            employee.DifferentlyAbledType.ToString(),
            employee.AddressLine1,
            employee.AddressLine2,
            employee.City,
            employee.ResidentialState?.ToString(),
            employee.PinCode,
            employee.PaymentMode.ToString(),
            employee.AccountHolderName,
            employee.BankName,
            employee.AccountType?.ToString(),
            maskedAcct,
            employee.UAN,
            employee.ESICIPNumber,
            employee.EpfEnabled,
            employee.EsiEnabled,
            employee.PtEnabled,
            employee.LwfEnabled,
            employee.IsPWD,
            maskedPAN);
    }
}
