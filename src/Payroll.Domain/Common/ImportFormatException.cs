namespace Payroll.Domain.Common;

public sealed class ImportFormatException(string message) : Exception(message);
