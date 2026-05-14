namespace Payroll.Infrastructure.Email;

public sealed class EmailSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public bool UseStartTls { get; init; } = true;
    public string BaseUrl { get; init; } = string.Empty;
}
