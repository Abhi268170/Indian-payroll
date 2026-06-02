using Payroll.Domain.Entities;

namespace Payroll.Application.Interfaces;

// WI-31: generates the relieving / experience letter PDF for an exiting employee.
public interface IExitDocumentGenerator
{
    byte[] GenerateRelievingLetter(Employee employee, EmployeeExit exit, string companyName, string tenureLabel);
}
