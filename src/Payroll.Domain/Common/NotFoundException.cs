namespace Payroll.Domain.Common;

public sealed class NotFoundException(string message) : DomainException(message) { }
