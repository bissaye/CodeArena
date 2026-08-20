using CodeArena.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CodeArena.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _fromAddress;
    private readonly string _appUrl;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
        _smtpHost = config["SMTP_HOST"] ?? "smtp-relay.brevo.com";
        _smtpPort = int.TryParse(config["SMTP_PORT"], out var port) ? port : 587;
        _smtpUser = config["SMTP_USER"] ?? string.Empty;
        _smtpPassword = config["SMTP_PASSWORD"] ?? string.Empty;
        _fromAddress = config["SMTP_FROM"] ?? "CodeArena <noreply@bissaye.online>";
        _appUrl = config["APP_URL"] ?? "https://codearena.bissaye.online";
    }

    public async Task SendEmailVerificationAsync(string toEmail, string username, string token, CancellationToken ct = default)
    {
        var verifyUrl = $"{_appUrl}/verify-email?token={token}";
        var subject = "Vérifiez votre adresse email — CodeArena";
        var body = EmailTemplates.BuildVerificationEmail(username, verifyUrl);
        await SendAsync(toEmail, username, subject, body, ct);
        _logger.LogInformation("Email de vérification envoyé à {Username}", username);
    }

    public async Task SendPasswordResetAsync(string toEmail, string username, string token, CancellationToken ct = default)
    {
        var resetUrl = $"{_appUrl}/reset-password?token={token}";
        var subject = "Réinitialisation de votre mot de passe — CodeArena";
        var body = EmailTemplates.BuildPasswordResetEmail(username, resetUrl);
        await SendAsync(toEmail, username, subject, body, ct);
        _logger.LogInformation("Email de réinitialisation envoyé à {Username}", username);
    }

    public async Task SendWelcomeAsync(string toEmail, string username, CancellationToken ct = default)
    {
        var subject = "Bienvenue sur CodeArena Cameroun !";
        var body = EmailTemplates.BuildWelcomeEmail(username, _appUrl);
        await SendAsync(toEmail, username, subject, body, ct);
        _logger.LogInformation("Email de bienvenue envoyé à {Username}", username);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPassword))
        {
            _logger.LogWarning("SMTP non configuré — email non envoyé (destinataire: {Email})", toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_smtpUser, _smtpPassword, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
