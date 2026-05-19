using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Email;

internal sealed class SmtpEmailService(IOptions<EmailSettings> options) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using SmtpClient smtp = new();
        SecureSocketOptions socketOptions = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

        await smtp.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

        if (!string.IsNullOrEmpty(_settings.Username))
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);
    }

    public async Task SendWithAttachmentAsync(string to, string subject, string htmlBody, byte[] attachment, string attachmentName, string contentType, CancellationToken ct = default)
    {
        MimeMessage message = new();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        BodyBuilder builder = new() { HtmlBody = htmlBody };
        builder.Attachments.Add(attachmentName, attachment, ContentType.Parse(contentType));
        message.Body = builder.ToMessageBody();

        using SmtpClient smtp = new();
        SecureSocketOptions socketOptions = _settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

        await smtp.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

        if (!string.IsNullOrEmpty(_settings.Username))
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
